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
public class ExpensesController : ControllerBase
{
    private readonly AppDbContext _context;
    
    public ExpensesController(AppDbContext context)
    {
        _context = context;
    }
    
    // Helper method to get current user's ID from JWT token
    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return int.Parse(userIdClaim.Value);
    }
    
    // GET: api/expenses
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Expense>>> GetExpenses(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] decimal? minAmount,
        [FromQuery] decimal? maxAmount,
        [FromQuery] string? search,
        [FromQuery] int? categoryId,
        [FromQuery] int? paidById)
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var query = _context.Expenses
            .Where(e => e.HouseholdId == user.HouseholdId)
            .Include(e => e.Category)
            .Include(e => e.PaidByUserId)
            .AsQueryable();
        
        if (startDate.HasValue)
        {
            query = query.Where(e => e.ExpenseDate >= startDate.Value);
        }
        
        if (endDate.HasValue)
        {
            query = query.Where(e => e.ExpenseDate <= endDate.Value);
        }
        
        if (minAmount.HasValue)
        {
            query = query.Where(e => e.Amount >= minAmount.Value);
        }
        
        if (maxAmount.HasValue)
        {
            query = query.Where(e => e.Amount <= maxAmount.Value);
        }
        
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(e => e.Description.ToLower().Contains(search.ToLower()));
        }
        
        if (categoryId.HasValue)
        {
            query = query.Where(e => e.CategoryId == categoryId.Value);
        }
        
        if (paidById.HasValue)
        {
            query = query.Where(e => e.PaidByUserId == paidById.Value);
        }
        
        var expenses = await query
            .OrderByDescending(e => e.ExpenseDate)
            .ToListAsync();
        
        return Ok(expenses);
    }
    
    // GET: api/expenses/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Expense>> GetExpense(int id)
    {
        var userId = GetCurrentUserId();
        
        // Get user's household
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var expense = await _context.Expenses
            .Include(e => e.Category)
            .Include(e => e.Household)
            .Include(e => e.PaidByUser)
            .FirstOrDefaultAsync(e => e.Id == id && e.HouseholdId == user.HouseholdId);
        
        if (expense == null)
        {
            return NotFound(new { message = "Expense not found" });
        }
        
        return Ok(expense);
    }
    
    // GET: api/expenses/summary
    [HttpGet("summary")]
    public async Task<ActionResult> GetSummary()
    {
        var userId = GetCurrentUserId();
        
        // Get user's household
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var expenses = await _context.Expenses
            .Where(e => e.HouseholdId == user.HouseholdId)
            .Include(e => e.Category)
            .ToListAsync();
        
        var totalSpent = expenses.Sum(e => e.Amount);

        var byCategory = expenses
            .GroupBy(e => e.Category.Name)
            .Select(g => new
            {
                Category = g.Key,
                Total = g.Sum(e => e.Amount),
                Count = g.Count()
            })
            .OrderByDescending(x => x.Total)
            .ToList();

        var thisMonth = expenses
            .Where(e => e.ExpenseDate.Month == DateTime.UtcNow.Month
                     && e.ExpenseDate.Year == DateTime.UtcNow.Year)
            .Sum(e => e.Amount);

        return Ok(new
        {
            TotalSpent = totalSpent,
            ThisMonth = thisMonth,
            ByCategory = byCategory,
            TotalExpenses = expenses.Count
        });
    }
    
    // POST: api/expenses
    [HttpPost]
    public async Task<ActionResult<Expense>> CreateExpense([FromBody] CreateExpenseRequest request)
    {
        var userId = GetCurrentUserId();
        
        // Get user's household
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household to create expenses" });
        }
        
        var expense = new Expense
        {
            PaidByUserId = userId,
            HouseholdId = user.HouseholdId.Value,
            CategoryId = request.CategoryId,
            Description = request.Description,
            Amount = request.Amount,
            ExpenseDate = request.ExpenseDate,
            Notes = request.Notes ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync();
        
        // Load relationships for response
        await _context.Entry(expense).Reference(e => e.Category).LoadAsync();
        await _context.Entry(expense).Reference(e => e.Household).LoadAsync();
        await _context.Entry(expense).Reference(e => e.PaidByUser).LoadAsync();
        
        return CreatedAtAction(nameof(GetExpense), new { id = expense.Id }, expense);
    }
    
    // PUT: api/expenses/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateExpense(int id, [FromBody] CreateExpenseRequest request)
    {
        var userId = GetCurrentUserId();
        
        // Get user's household
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var expense = await _context.Expenses
            .FirstOrDefaultAsync(e => e.Id == id && e.HouseholdId == user.HouseholdId);
        
        if (expense == null)
        {
            return NotFound(new { message = "Expense not found" });
        }
        
        // Update fields
        expense.CategoryId = request.CategoryId;
        expense.Description = request.Description;
        expense.Amount = request.Amount;
        expense.ExpenseDate = request.ExpenseDate;
        expense.Notes = request.Notes ?? string.Empty;
        expense.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        return NoContent();
    }
    
    // DELETE: api/expenses/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteExpense(int id)
    {
        var userId = GetCurrentUserId();
        
        // Get user's household
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var expense = await _context.Expenses
            .FirstOrDefaultAsync(e => e.Id == id && e.HouseholdId == user.HouseholdId);
        
        if (expense == null)
        {
            return NotFound(new { message = "Expense not found" });
        }
        
        _context.Expenses.Remove(expense);
        await _context.SaveChangesAsync();
        
        return NoContent();
    }
}

// DTO for creating/updating expenses
public class CreateExpenseRequest
{
    public int CategoryId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string? Notes { get; set; }
}