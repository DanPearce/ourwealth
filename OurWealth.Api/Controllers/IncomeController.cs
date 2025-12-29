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

public class IncomeController : ControllerBase
{
    private readonly AppDbContext _context;
    
    public IncomeController(AppDbContext context)
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
    
    // GET: api/income
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Income>>> GetIncome([FromQuery] int? month, [FromQuery] int? year, [FromQuery] int? userId)
    {
        var currentUserId = GetCurrentUserId();
        
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == currentUserId);

        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        // Household filter
        var query = _context.Incomes
            .Where(i => i.HouseholdId == user.HouseholdId);
        
        // Filter by userId if provided
        if (userId.HasValue)
        {
            query = query.Where(i => i.UserId == userId.Value);
        }
        
        // Filter by month and year if provided
        if (month.HasValue)
        {
            query = query.Where(i => i.Month == month.Value);
        }
        if (year.HasValue)
        {
            query = query.Where(i => i.Year == year.Value);
        }

        var incomes = await query
            .Include(i => i.User)
            .OrderByDescending(i => i.Year)
            .ThenByDescending(i => i.Month)
            .ToListAsync();
        
        return Ok(incomes);
    }
    
    // GET: api/income/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Income>> GetIncome(int id)
    {
        var currentUserId = GetCurrentUserId();
        
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == currentUserId);

        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var income = await _context.Incomes
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.Id == id && i.HouseholdId == user.HouseholdId);

        if (income == null)
        {
            return NotFound(new {message = "Income not found"});
        }
        
        return Ok(income);
    }
    
    // POST: api/income
    [HttpPost]
    public async Task<ActionResult<Income>> CreateIncome([FromBody] CreateIncomeRequest request)
    {
        var currentUserId = GetCurrentUserId();
        
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == currentUserId);

        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household to create income" });
        }

        var income = new Income
        {
            UserId = user.Id,
            HouseholdId = user.HouseholdId.Value,
            Month = request.Month,
            Year = request.Year,
            Amount = request.Amount,
            Source = request.Source,
            RecievedDate = request.RecievedDate,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.Incomes.Add(income);
        await _context.SaveChangesAsync();
        
        // Load user for response
        await _context.Entry(income).Reference(i => i.User).LoadAsync();
        
        return CreatedAtAction(nameof(GetIncome), new { id = income.Id }, income);
    }
    
    // PUT: api/income/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateIncome(int id, [FromBody] CreateIncomeRequest request)
    {
        var currentUserId = GetCurrentUserId();
    
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == currentUserId);
    
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
    
        var income = await _context.Incomes
            .FirstOrDefaultAsync(i => i.Id == id && i.HouseholdId == user.HouseholdId);
    
        if (income == null)
        {
            return NotFound(new { message = "Income not found" });
        }
    
        // Update fields
        income.Month = request.Month;
        income.Year = request.Year;
        income.Amount = request.Amount;
        income.Source = request.Source;
        income.RecievedDate = request.RecievedDate;
    
        await _context.SaveChangesAsync();
    
        return NoContent();
    }
    
    // DELETE: api/income/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteIncome(int id)
    {
        var currentUserId = GetCurrentUserId();
    
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == currentUserId);
    
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
    
        var income = await _context.Incomes
            .FirstOrDefaultAsync(i => i.Id == id && i.HouseholdId == user.HouseholdId);
    
        if (income == null)
        {
            return NotFound(new { message = "Income not found" });
        }
    
        _context.Incomes.Remove(income);
        await _context.SaveChangesAsync();
    
        return NoContent();
    }
    
    // DTO for creating/updating income
    public class CreateIncomeRequest
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal Amount { get; set; }
        public string Source { get; set; } = string.Empty;
        public DateTime? RecievedDate { get; set; }
    }
}