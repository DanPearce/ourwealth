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
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReportsController(AppDbContext context)
    {
        _context = context;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            throw new UnauthorizedAccessException("User ID not found in token.");
        }

        return int.Parse(userIdClaim.Value);
    }

    // GET: api/reports/monthly-summary?month=12&year=2025
    [HttpGet("monthly-summary")]
    public async Task<ActionResult<MonthlySummaryResponse>> GetMonthlySummary(
        [FromQuery] int month,
        [FromQuery] int year)
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household." });
        }

        var householdId = user.HouseholdId.Value;

        var totalIncome = await _context.Incomes
            .Where(i => i.HouseholdId == householdId
                        && i.Month == month
                        && i.Year == year)
            .SumAsync(i => i.Amount);

        var totalExpenses = await _context.Expenses
            .Where(e => e.HouseholdId == householdId
                        && e.ExpenseDate.Month == month
                        && e.ExpenseDate.Year == year)
            .SumAsync(e => e.Amount);

        var expensesByCategory = await _context.Expenses
            .Where(e => e.HouseholdId == householdId
                        && e.ExpenseDate.Month == month
                        && e.ExpenseDate.Year == year)
            .GroupBy(e => new { e.Category.Id, e.Category.Name, e.Category.Color })
            .Select(g => new CategoryBreakdown
            {
                CategoryId = g.Key.Id,                CategoryName = g.Key.Name,
                CategoryColor = g.Key.Color,
                TotalAmount = g.Sum(e => e.Amount),
                TransactionCount = g.Count(),
                AverageAmount = g.Average(e => e.Amount)
            })
            .OrderByDescending(c => c.TotalAmount)
            .ToListAsync();

        var budgets = await _context.Budgets
            .Where(b => b.HouseholdId == householdId
                        && b.Month == month
                        && b.Year == year)
            .Include(b => b.Category)
            .ToListAsync();

        var budgetComparision = budgets.Select(budget =>
        {
            var spent = expensesByCategory
                .FirstOrDefault(e => e.CategoryId == budget.CategoryId)?.TotalAmount ?? 0;

            return new BudgetComparison
            {
                CategoryId = budget.CategoryId,
                CategoryName = budget.Category?.Name ?? "Uncategorized",
                BudgetAmount = budget.Amount,
                SpentAmount = spent,
                RemainingAmount = budget.Amount - spent,
                PercentUsed = budget.Amount > 0 ? (spent / budget.Amount) * 100 : 0,
                IsOverBudget = spent > budget.Amount
            };
        }).ToList();

        var response = new MonthlySummaryResponse
        {
            Month = month,
            Year = year,
            TotalIncome = totalIncome,
            TotalExpenses = totalExpenses,
            NetIncome = totalIncome - totalExpenses,
            ExpensesByCategory = expensesByCategory,
            BudgetComparison = budgetComparision,
            TransactionCount = await _context.Expenses
                .CountAsync(e => e.HouseholdId == householdId
                                 && e.ExpenseDate.Month == month
                                 && e.ExpenseDate.Year == year)
        };
        return Ok(response);
    }
    
    // GET: api/reports/spending-by-category?month=12&year=2025
    [HttpGet("spending-by-category")]
    public async Task<ActionResult<List<CategoryBreakdown>>> GetSpendingByCategory(
        [FromQuery] int month,
        [FromQuery] int year)
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household." });
        }
        
        var householdId = user.HouseholdId.Value;
        
        var expensesByCategory = await _context.Expenses
            .Where(e => e.HouseholdId == householdId
                        && e.ExpenseDate.Month == month
                        && e.ExpenseDate.Year == year)
            .GroupBy(e => new { e.Category.Id, e.Category.Name, e.Category.Color })
            .Select(g => new CategoryBreakdown
            {
                CategoryId = g.Key.Id,
                CategoryName = g.Key.Name,
                CategoryColor = g.Key.Color,
                TotalAmount = g.Sum(e => e.Amount),
                TransactionCount = g.Count(),
                AverageAmount = g.Average(e => e.Amount)
            })
            .OrderByDescending(c => c.TotalAmount)
            .ToListAsync();
        
        return Ok(expensesByCategory);
    }
    
    // GET: api/reports/budget-comparison?month=12&year=2025
    [HttpGet("budget-comparison")]
    public async Task<ActionResult<List<BudgetComparison>>> GetBudgetComparison(
        [FromQuery] int month,
        [FromQuery] int year)
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household." });
        }
        
        var householdId = user.HouseholdId.Value;
        
        var expenses = await _context.Expenses
            .Where(e => e.HouseholdId == householdId
                             && e.ExpenseDate.Month == month
                             && e.ExpenseDate.Year == year)
            .GroupBy(e => e.CategoryId)
            .Select(g => new
            {
                CategoryId = g.Key,
                TotalSpent = g.Sum(e => e.Amount)
            })
            .ToDictionaryAsync(e => e.CategoryId, e => e.TotalSpent);
        
        var budgetComparision = await _context.Budgets
            .Where(b => b.HouseholdId == householdId
                        && b.Month == month
                        && b.Year == year)
            .Include(b => b.Category)
            .Select(budget => new BudgetComparison
            {
                CategoryId = budget.CategoryId,
                CategoryName = budget.Category != null ? budget.Category.Name : "Uncategorized",
                BudgetAmount = budget.Amount,
                SpentAmount = expenses.ContainsKey(budget.CategoryId ?? 0) 
                    ? expenses[budget.CategoryId ?? 0] 
                    : 0,
                RemainingAmount = budget.Amount - (expenses.ContainsKey(budget.CategoryId ?? 0) 
                    ? expenses[budget.CategoryId ?? 0] 
                    : 0),
                PercentUsed = budget.Amount > 0 
                    ? ((expenses.ContainsKey(budget.CategoryId ?? 0) 
                        ? expenses[budget.CategoryId ?? 0] 
                        : 0) / budget.Amount) * 100 
                    : 0,
                IsOverBudget = (expenses.ContainsKey(budget.CategoryId ?? 0) 
                    ? expenses[budget.CategoryId ?? 0] 
                    : 0) > budget.Amount
            })
            .ToListAsync();
        return Ok(budgetComparision);
    }
    
    // GET: api/reports/month-comparison?month1=11&year1=2024&month2=12&year2=2024
    [HttpGet("month-comparison")]
    public async Task<ActionResult<MonthComparisonResponse>> GetMonthComparison(
        [FromQuery] int month1,
        [FromQuery] int year1,
        [FromQuery] int month2,
        [FromQuery] int year2)
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household." });
        }
        
        var householdId = user.HouseholdId.Value;
        
        async Task<MonthData> GetMonthData(int month, int year)
        {
            var income = await _context.Incomes
                .Where(i => i.HouseholdId == householdId 
                         && i.Month == month 
                         && i.Year == year)
                .SumAsync(i => i.Amount);
            
            var expenses = await _context.Expenses
                .Where(e => e.HouseholdId == householdId
                         && e.ExpenseDate.Month == month
                         && e.ExpenseDate.Year == year)
                .SumAsync(e => e.Amount);
            
            var count = await _context.Expenses
                .CountAsync(e => e.HouseholdId == householdId
                              && e.ExpenseDate.Month == month
                              && e.ExpenseDate.Year == year);
            
            return new MonthData
            {
                Month = month,
                Year = year,
                TotalIncome = income,
                TotalExpenses = expenses,
                NetIncome = income - expenses,
                TransactionCount = count
            };
        }
        
        var monthData1 = await GetMonthData(month1, year1);
        var monthData2 = await GetMonthData(month2, year2);
        
        var incomeDiff = monthData2.TotalIncome - monthData1.TotalIncome;
        var expenseDiff = monthData2.TotalExpenses - monthData1.TotalExpenses;
        
        var incomeChangePercent = monthData1.TotalIncome > 0 
            ? (incomeDiff / monthData1.TotalIncome) * 100 
            : 0;
        
        var expenseChangePercent = monthData1.TotalExpenses > 0 
            ? (expenseDiff / monthData1.TotalExpenses) * 100 
            : 0;
        
        string trend;
        if (Math.Abs(expenseChangePercent) < 5)
            trend = "Stable";
        else if (expenseChangePercent > 0)
            trend = "Spending Up";
        else
            trend = "Spending Down";
        
        var response = new MonthComparisonResponse
        {
            Month1 = monthData1,
            Month2 = monthData2,
            Comparison = new ComparisonMetrics
            {
                IncomeDifference = incomeDiff,
                ExpenseDifference = expenseDiff,
                IncomeChangePercent = incomeChangePercent,
                ExpenseChangePercent = expenseChangePercent,
                Trend = trend
            }
        };
        
        return Ok(response);
    }
}

// DTOs for the response
public class MonthlySummaryResponse
{
    public int Month { get; set; }
    public int Year { get; set; }
    
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetIncome { get; set; }  
    public int TransactionCount { get; set; }
    
    public List<CategoryBreakdown> ExpensesByCategory { get; set; } = new();
    public List<BudgetComparison> BudgetComparison { get; set; } = new();
}

public class CategoryBreakdown
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = string.Empty;
    
    public decimal TotalAmount { get; set; }      
    public int TransactionCount { get; set; }    
    public decimal AverageAmount { get; set; }  
}

public class BudgetComparison
{
    public int? CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    
    public decimal BudgetAmount { get; set; }     
    public decimal SpentAmount { get; set; }      
    public decimal RemainingAmount { get; set; } 
    public decimal PercentUsed { get; set; }     
    public bool IsOverBudget { get; set; }
}

public class MonthComparisonResponse
{
    public MonthData Month1 { get; set; } = new();
    public MonthData Month2 { get; set; } = new();
    public ComparisonMetrics Comparison { get; set; } = new();
}

public class MonthData
{
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetIncome { get; set; }
    public int TransactionCount { get; set; }
}

public class ComparisonMetrics
{
    public decimal IncomeDifference { get; set; }
    public decimal ExpenseDifference { get; set; }
    public decimal IncomeChangePercent { get; set; }
    public decimal ExpenseChangePercent { get; set; }
    public string Trend { get; set; } = string.Empty;
}