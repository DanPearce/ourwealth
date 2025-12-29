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
public class DebtsController : ControllerBase
{
    private readonly AppDbContext _context;
    
    public DebtsController(AppDbContext context)
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
    
    // GET: api/debts
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Debt>>> GetDebts([FromQuery] bool? isActive)
    {
        var userId = GetCurrentUserId();
        
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var query = _context.Debts
            .Where(d => d.HouseholdId == user.HouseholdId);
        
        if (isActive.HasValue)
        {
            query = query.Where(d => d.IsActive == isActive.Value);
        }
        
        var debts = await query
            .OrderByDescending(d => d.CurrentBalance)
            .ToListAsync();
        
        return Ok(debts);
    }
    
    // GET: api/debts/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Debt>> GetDebt(int id)
    {
        var userId = GetCurrentUserId();
        
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var debt = await _context.Debts
            .Include(d => d.DebtPayments)
            .FirstOrDefaultAsync(d => d.Id == id && d.HouseholdId == user.HouseholdId);
        
        if (debt == null)
        {
            return NotFound(new { message = "Debt not found" });
        }
        
        return Ok(debt);
    }
    
    // POST: api/debts
    [HttpPost]
    public async Task<ActionResult<Debt>> CreateDebt([FromBody] CreateDebtRequest request)
    {
        var userId = GetCurrentUserId();
        
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household to create debts" });
        }
        
        var debt = new Debt
        {
            HouseholdId = user.HouseholdId.Value,
            Name = request.Name,
            DebtType = request.DebtType,
            OriginalAmount = request.OriginalAmount,
            CurrentBalance = request.CurrentBalance,
            InterestRate = request.InterestRate,
            MinimumPayment = request.MinimumPayment,
            PaymentDayOfMonth = request.PaymentDayOfMonth,
            Creditor = request.Creditor ?? string.Empty,
            IsActive = true,
            Notes = request.Notes ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _context.Debts.Add(debt);
        await _context.SaveChangesAsync();
        
        return CreatedAtAction(nameof(GetDebt), new { id = debt.Id }, debt);
    }
    
    // PUT: api/debts/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDebt(int id, [FromBody] CreateDebtRequest request)
    {
        var userId = GetCurrentUserId();
        
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var debt = await _context.Debts
            .FirstOrDefaultAsync(d => d.Id == id && d.HouseholdId == user.HouseholdId);
        
        if (debt == null)
        {
            return NotFound(new { message = "Debt not found" });
        }
        
        debt.Name = request.Name;
        debt.DebtType = request.DebtType;
        debt.OriginalAmount = request.OriginalAmount;
        debt.CurrentBalance = request.CurrentBalance;
        debt.InterestRate = request.InterestRate;
        debt.MinimumPayment = request.MinimumPayment;
        debt.PaymentDayOfMonth = request.PaymentDayOfMonth;
        debt.Creditor = request.Creditor ?? string.Empty;
        debt.Notes = request.Notes ?? string.Empty;
        debt.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        return NoContent();
    }
    
    // DELETE: api/debts/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDebt(int id)
    {
        var userId = GetCurrentUserId();
        
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var debt = await _context.Debts
            .FirstOrDefaultAsync(d => d.Id == id && d.HouseholdId == user.HouseholdId);
        
        if (debt == null)
        {
            return NotFound(new { message = "Debt not found" });
        }
        
        debt.IsActive = false;
        debt.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        
        return NoContent();
    }
}

public class CreateDebtRequest
{
    public string Name { get; init; } = string.Empty;
    public string DebtType { get; init; } = string.Empty;
    public decimal OriginalAmount { get; init; }
    public decimal CurrentBalance { get; init; }
    public decimal? InterestRate { get; init; }
    public decimal? MinimumPayment { get; init; }
    public int? PaymentDayOfMonth { get; init; }
    public string? Creditor { get; init; }
    public string? Notes { get; init; }
}