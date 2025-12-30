#nullable enable
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OurWealth.Api.Models;
using OurWealth.Api.Data;
using System.Security.Claims;

namespace OurWealth.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;
    
    public DashboardController(AppDbContext context)
    {
        _context = context;
    }
    
    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return int.Parse(userIdClaim.Value);
    }
    
    // GET: api/dashboard
    [HttpGet]
    public async Task<ActionResult<DashboardResponse>> GetDashboard([FromQuery] int? month, [FromQuery] int? year)
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        // Default to current month/year if not specified
        var targetMonth = month ?? DateTime.UtcNow.Month;
        var targetYear = year ?? DateTime.UtcNow.Year;
        
        var householdId = user.HouseholdId.Value;
        
        // Get all data sequentially (DbContext doesn't support parallel operations)
        var totalIncome = await GetMonthlyIncome(householdId, targetMonth, targetYear);
        var expenseData = await GetMonthlyExpenses(householdId, targetMonth, targetYear);
        var budgets = await GetBudgetProgress(householdId, targetMonth, targetYear);
        var debts = await GetDebtSummary(householdId);
        var savings = await GetSavingsProgress(householdId);
        var upcomingBills = await GetUpcomingBills(householdId);
        var recentExpenses = await GetRecentExpenses(householdId, 5);
        var settlementBalance = await GetSettlementBalance(userId, householdId);
        
        var response = new DashboardResponse
        {
            Month = targetMonth,
            Year = targetYear,
            TotalIncome = totalIncome,
            TotalExpenses = expenseData.Total,
            ExpensesByCategory = expenseData.ByCategory,
            NetIncome = totalIncome - expenseData.Total,
            Budgets = budgets,
            Debts = debts,
            Savings = savings,
            UpcomingBills = upcomingBills,
            RecentExpenses = recentExpenses,
            SettlementBalance = settlementBalance
        };
        
        return Ok(response);
    }
    
    private async Task<decimal> GetMonthlyIncome(int householdId, int month, int year)
    {
        return await _context.Incomes
            .Where(i => i.HouseholdId == householdId && i.Month == month && i.Year == year)
            .SumAsync(i => i.Amount);
    }
    
    private async Task<(decimal Total, List<CategoryExpense> ByCategory)> GetMonthlyExpenses(int householdId, int month, int year)
    {
        var expenses = await _context.Expenses
            .Include(e => e.Category)
            .Where(e => e.HouseholdId == householdId 
                && e.ExpenseDate.Month == month 
                && e.ExpenseDate.Year == year)
            .ToListAsync();
        
        var total = expenses.Sum(e => e.Amount);
        
        var byCategory = expenses
            .Where(e => e.Category != null)
            .GroupBy(e => new { e.CategoryId, CategoryName = e.Category!.Name, CategoryColor = e.Category!.Color })
            .Select(g => new CategoryExpense
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.CategoryName,
                CategoryColor = g.Key.CategoryColor,
                Amount = g.Sum(e => e.Amount),
                Count = g.Count()
            })
            .OrderByDescending(c => c.Amount)
            .ToList();
        
        return (total, byCategory);
    }
    
    private async Task<List<BudgetProgress>> GetBudgetProgress(int householdId, int month, int year)
    {
        var budgets = await _context.Budgets
            .Include(b => b.Category)
            .Where(b => b.HouseholdId == householdId && b.Month == month && b.Year == year)
            .ToListAsync();
        
        var expenses = await _context.Expenses
            .Where(e => e.HouseholdId == householdId 
                && e.ExpenseDate.Month == month 
                && e.ExpenseDate.Year == year)
            .ToListAsync();
        
        var budgetProgress = new List<BudgetProgress>();
        
        foreach (var budget in budgets)
        {
            decimal spent;
            
            if (budget.CategoryId == null)
            {
                // Total budget
                spent = expenses.Sum(e => e.Amount);
            }
            else
            {
                // Category budget
                spent = expenses.Where(e => e.CategoryId == budget.CategoryId).Sum(e => e.Amount);
            }
            
            var remaining = budget.Amount - spent;
            var percentUsed = budget.Amount > 0 ? (spent / budget.Amount) * 100 : 0;
            
            budgetProgress.Add(new BudgetProgress
            {
                BudgetId = budget.Id,
                CategoryId = budget.CategoryId,
                CategoryName = budget.Category?.Name ?? "Total Budget",
                BudgetAmount = budget.Amount,
                SpentAmount = spent,
                RemainingAmount = remaining,
                PercentUsed = Math.Round(percentUsed, 2),
                IsOverBudget = spent > budget.Amount
            });
        }
        
        return budgetProgress.OrderByDescending(b => b.PercentUsed).ToList();
    }
    
    private async Task<DebtSummary> GetDebtSummary(int householdId)
    {
        var debts = await _context.Debts
            .Where(d => d.HouseholdId == householdId && d.IsActive)
            .ToListAsync();
        
        var totalOriginal = debts.Sum(d => d.OriginalAmount);
        var totalCurrent = debts.Sum(d => d.CurrentBalance);
        var totalPaid = totalOriginal - totalCurrent;
        var percentPaid = totalOriginal > 0 ? (totalPaid / totalOriginal) * 100 : 0;
        
        var debtList = debts.Select(d => new DebtItem
        {
            Id = d.Id,
            Name = d.Name,
            DebtType = d.DebtType,
            CurrentBalance = d.CurrentBalance,
            InterestRate = d.InterestRate,
            MinimumPayment = d.MinimumPayment
        }).OrderByDescending(d => d.CurrentBalance).ToList();
        
        return new DebtSummary
        {
            TotalDebt = totalCurrent,
            TotalPaid = totalPaid,
            PercentPaid = Math.Round(percentPaid, 2),
            Debts = debtList
        };
    }
    
    private async Task<SavingsSummary> GetSavingsProgress(int householdId)
    {
        var goals = await _context.SavingsGoals
            .Where(sg => sg.HouseholdId == householdId && sg.IsActive)
            .ToListAsync();
        
        var totalTarget = goals.Sum(g => g.TargetAmount);
        var totalCurrent = goals.Sum(g => g.CurrentAmount);
        var totalRemaining = totalTarget - totalCurrent;
        var percentComplete = totalTarget > 0 ? (totalCurrent / totalTarget) * 100 : 0;
        
        var goalList = goals.Select(g => new SavingsGoalItem
        {
            Id = g.Id,
            Name = g.Name,
            TargetAmount = g.TargetAmount,
            CurrentAmount = g.CurrentAmount,
            RemainingAmount = g.TargetAmount - g.CurrentAmount,
            PercentComplete = g.TargetAmount > 0 ? Math.Round((g.CurrentAmount / g.TargetAmount) * 100, 2) : 0,
            TargetDate = g.TargetDate,
            Priority = g.Priority
        }).OrderByDescending(g => g.PercentComplete).ToList();
        
        return new SavingsSummary
        {
            TotalSaved = totalCurrent,
            TotalTarget = totalTarget,
            TotalRemaining = totalRemaining,
            PercentComplete = Math.Round(percentComplete, 2),
            Goals = goalList
        };
    }
    
    private async Task<List<UpcomingBill>> GetUpcomingBills(int householdId)
    {
        var today = DateTime.UtcNow;
        var currentMonth = today.Month;
        var currentYear = today.Year;
        var currentDay = today.Day;
        
        var activeBills = await _context.RecurringBills
            .Include(rb => rb.Category)
            .Where(rb => rb.HouseholdId == householdId && rb.IsActive)
            .ToListAsync();
        
        var upcomingBills = new List<UpcomingBill>();
        
        foreach (var bill in activeBills)
        {
            if (bill.DayOfMonth.HasValue)
            {
                var dueDay = bill.DayOfMonth.Value;
                
                // Check if already paid this month
                var paidThisMonth = await _context.BillPayments
                    .AnyAsync(bp => bp.RecurringBillId == bill.Id 
                        && bp.Month == currentMonth 
                        && bp.Year == currentYear);
                
                if (!paidThisMonth)
                {
                    // Calculate due date
                    DateTime dueDate;
                    if (dueDay >= currentDay)
                    {
                        // Due this month
                        dueDate = new DateTime(currentYear, currentMonth, Math.Min(dueDay, DateTime.DaysInMonth(currentYear, currentMonth)));
                    }
                    else
                    {
                        // Due next month
                        var nextMonth = currentMonth == 12 ? 1 : currentMonth + 1;
                        var nextYear = currentMonth == 12 ? currentYear + 1 : currentYear;
                        dueDate = new DateTime(nextYear, nextMonth, Math.Min(dueDay, DateTime.DaysInMonth(nextYear, nextMonth)));
                    }
                    
                    var daysUntilDue = (dueDate - today).Days;
                    
                    // Only show bills due in next 30 days
                    if (daysUntilDue <= 30)
                    {
                        upcomingBills.Add(new UpcomingBill
                        {
                            BillId = bill.Id,
                            Description = bill.Description,
                            Amount = bill.Amount,
                            IsVariableAmount = bill.IsVariableAmount,
                            DueDate = dueDate,
                            DaysUntilDue = daysUntilDue,
                            CategoryName = bill.Category.Name,
                            IsOverdue = daysUntilDue < 0
                        });
                    }
                }
            }
        }
        
        return upcomingBills.OrderBy(b => b.DaysUntilDue).ToList();
    }
    
    private async Task<List<RecentExpenseItem>> GetRecentExpenses(int householdId, int count)
    {
        var expenses = await _context.Expenses
            .Include(e => e.Category)
            .Include(e => e.PaidByUser)
            .Where(e => e.HouseholdId == householdId)
            .OrderByDescending(e => e.ExpenseDate)
            .Take(count)
            .ToListAsync();
        
        return expenses.Select(e => new RecentExpenseItem
        {
            Id = e.Id,
            Amount = e.Amount,
            Description = e.Description,
            Date = e.ExpenseDate,
            CategoryName = e.Category.Name,
            CategoryColor = e.Category.Color,
            PaidByName = e.PaidByUser.DisplayName
        }).ToList();
    }
    
    private async Task<SettlementBalanceInfo> GetSettlementBalance(int userId, int householdId)
    {
        var settlements = await _context.Settlements
            .Where(s => s.HouseholdId == householdId)
            .ToListAsync();
        
        var owedToMe = settlements.Where(s => s.ToUserId == userId).Sum(s => s.Amount);
        var iOwe = settlements.Where(s => s.FromUserId == userId).Sum(s => s.Amount);
        var netBalance = owedToMe - iOwe;
        
        return new SettlementBalanceInfo
        {
            OwedToMe = owedToMe,
            IOwe = iOwe,
            NetBalance = netBalance,
            Status = netBalance > 0 ? "You are owed" : netBalance < 0 ? "You owe" : "All settled"
        };
    }
}

// Response DTOs
public class DashboardResponse
{
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public List<CategoryExpense> ExpensesByCategory { get; set; } = new();
    public decimal NetIncome { get; set; }
    public List<BudgetProgress> Budgets { get; set; } = new();
    public DebtSummary Debts { get; set; } = new();
    public SavingsSummary Savings { get; set; } = new();
    public List<UpcomingBill> UpcomingBills { get; set; } = new();
    public List<RecentExpenseItem> RecentExpenses { get; set; } = new();
    public SettlementBalanceInfo SettlementBalance { get; set; } = new();
}

public class CategoryExpense
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Count { get; set; }
}

public class BudgetProgress
{
    public int BudgetId { get; set; }
    public int? CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal BudgetAmount { get; set; }
    public decimal SpentAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal PercentUsed { get; set; }
    public bool IsOverBudget { get; set; }
}

public class DebtSummary
{
    public decimal TotalDebt { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal PercentPaid { get; set; }
    public List<DebtItem> Debts { get; set; } = new();
}

public class DebtItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DebtType { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    public decimal? InterestRate { get; set; }
    public decimal? MinimumPayment { get; set; }
}

public class SavingsSummary
{
    public decimal TotalSaved { get; set; }
    public decimal TotalTarget { get; set; }
    public decimal TotalRemaining { get; set; }
    public decimal PercentComplete { get; set; }
    public List<SavingsGoalItem> Goals { get; set; } = new();
}

public class SavingsGoalItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal PercentComplete { get; set; }
    public DateTime? TargetDate { get; set; }
    public string Priority { get; set; } = string.Empty;
}

public class UpcomingBill
{
    public int BillId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    public bool IsVariableAmount { get; set; }
    public DateTime DueDate { get; set; }
    public int DaysUntilDue { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsOverdue { get; set; }
}

public class RecentExpenseItem
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = string.Empty;
    public string PaidByName { get; set; } = string.Empty;
}

public class SettlementBalanceInfo
{
    public decimal OwedToMe { get; set; }
    public decimal IOwe { get; set; }
    public decimal NetBalance { get; set; }
    public string Status { get; set; } = string.Empty;
}