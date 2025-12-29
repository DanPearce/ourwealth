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

public class BudgetsController : ControllerBase
{
    private readonly AppDbContext _context;
    
    public BudgetsController(AppDbContext context)
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
    
    // GET: api/budgets
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Budget>>> GetBudgets([FromQuery] int? month, [FromQuery] int? year)
    {
        var userId = GetCurrentUserId();
        
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }

        var query = _context.Budgets
            .Where(b => b.HouseholdId == user.HouseholdId);
            
        if (month.HasValue)
        {
            query = query.Where(b => b.Month == month.Value);
        }
        if (year.HasValue)
        {
            query = query.Where(b => b.Year == year.Value);
        }
        var budgets = await query
            .Include(b => b.Category)    
            .OrderBy(b => b.Year)
            .ThenBy(b => b.Month)
            .ToListAsync();
        
        return Ok(budgets);
    }
    
    // GET: api/budgets/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Budget>> GetBudget(int id)
    {
        var userId = GetCurrentUserId();
        
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var budget = await _context.Budgets
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == id && b.HouseholdId == user.HouseholdId);

        if (budget == null)
        {
            return NotFound(new { message = "Budget not found" });
        }
        
        return Ok(budget);
    }
    
    // POST: api/budgets
    [HttpPost]
    public async Task<ActionResult<Budget>> CreateBudget([FromBody] CreateBudgetRequest request)
    {
        var userId = GetCurrentUserId();
        
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var budget = new Budget
        {
            HouseholdId = user.HouseholdId.Value,
            Month = request.Month,
            Year = request.Year,
            CategoryId = request.CategoryId,
            Amount = request.Amount,
            Date = DateTime.UtcNow
        };
        
        _context.Budgets.Add(budget);
        await _context.SaveChangesAsync();
        
        return CreatedAtAction(nameof(GetBudgets), new { id = budget.Id }, budget);
    }
    
    // DTO for creating a budget
    public class CreateBudgetRequest
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public int? CategoryId { get; set; }
        public decimal Amount { get; set; }
    }
    
    // PUT: api/budgets/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBudget(int id, [FromBody] CreateBudgetRequest request)
    {
        var userId = GetCurrentUserId();
        
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var budget = await _context.Budgets
            .FirstOrDefaultAsync(b => b.Id == id && b.HouseholdId == user.HouseholdId);
        if (budget == null)
        {
            return NotFound(new { message = "Budget not found" });
        }
        
        // Update fields
        budget.Month = request.Month;
        budget.Year = request.Year;
        budget.CategoryId = request.CategoryId;
        budget.Amount = request.Amount;
        
        await _context.SaveChangesAsync();
        
        return NoContent();
    }
    
    // DELETE: api/budgets/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBudget(int id)
    {
        var userId = GetCurrentUserId();
        
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user?.HouseholdId == null)
        {
            return BadRequest(new {message = "User must be part of a household"});
        }
        
        var budget = await _context.Budgets
            .FirstOrDefaultAsync(b => b.Id == id && b.HouseholdId == user.HouseholdId);
        if (budget == null)
        {
            return NotFound(new { message = "Budget not found" });
        }
        _context.Budgets.Remove(budget);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}