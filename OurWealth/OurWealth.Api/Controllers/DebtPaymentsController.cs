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
public class DebtPaymentsController : ControllerBase
{
    private readonly AppDbContext _context;
    
    public DebtPaymentsController(AppDbContext context)
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
    
    // GET: api/debtpayments
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DebtPayment>>> GetDebtPayments([FromQuery] int? debtId)
    {
        var userId = GetCurrentUserId();
        
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var query = _context.DebtPayments
            .Include(dp => dp.Debt)
            .Include(dp => dp.PaidBy)
            .Where(dp => dp.Debt.HouseholdId == user.HouseholdId);
        
        if (debtId.HasValue)
        {
            query = query.Where(dp => dp.DebtId == debtId.Value);
        }
        
        var payments = await query
            .OrderByDescending(dp => dp.PaymentDate)
            .ToListAsync();
        
        return Ok(payments);
    }
    
    // GET: api/debtpayments/5
    [HttpGet("{id}")]
    public async Task<ActionResult<DebtPayment>> GetDebtPayment(int id)
    {
        var userId = GetCurrentUserId();
        
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var payment = await _context.DebtPayments
            .Include(dp => dp.Debt)
            .Include(dp => dp.PaidBy)
            .FirstOrDefaultAsync(dp => dp.Id == id && dp.Debt.HouseholdId == user.HouseholdId);
        
        if (payment == null)
        {
            return NotFound(new { message = "Debt payment not found" });
        }
        
        return Ok(payment);
    }
    
    // POST: api/debtpayments
    [HttpPost]
    public async Task<ActionResult<DebtPayment>> CreateDebtPayment([FromBody] CreateDebtPaymentRequest request)
    {
        var userId = GetCurrentUserId();
        
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household to create debt payments" });
        }
        
        var debt = await _context.Debts
            .FirstOrDefaultAsync(d => d.Id == request.DebtId && d.HouseholdId == user.HouseholdId);
        
        if (debt == null)
        {
            return BadRequest(new { message = "Debt not found in your household" });
        }
        
        var payment = new DebtPayment
        {
            DebtId = request.DebtId,
            Amount = request.Amount,
            PaymentDate = request.PaymentDate,
            PaidById = request.PaidById ?? userId,
            Notes = request.Notes ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.DebtPayments.Add(payment);
        
        // Update debt balance
        debt.CurrentBalance -= request.Amount;
        debt.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        await _context.Entry(payment).Reference(dp => dp.Debt).LoadAsync();
        if (payment.PaidById.HasValue)
        {
            await _context.Entry(payment).Reference(dp => dp.PaidBy).LoadAsync();
        }
        
        return CreatedAtAction(nameof(GetDebtPayment), new { id = payment.Id }, payment);
    }
    
    // PUT: api/debtpayments/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDebtPayment(int id, [FromBody] CreateDebtPaymentRequest request)
    {
        var userId = GetCurrentUserId();
        
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var payment = await _context.DebtPayments
            .Include(dp => dp.Debt)
            .FirstOrDefaultAsync(dp => dp.Id == id && dp.Debt.HouseholdId == user.HouseholdId);
        
        if (payment == null)
        {
            return NotFound(new { message = "Debt payment not found" });
        }
        
        // Adjust debt balance (remove old amount, add new amount)
        var debt = payment.Debt;
        debt.CurrentBalance += payment.Amount;  // Add back old payment
        debt.CurrentBalance -= request.Amount;  // Subtract new payment
        debt.UpdatedAt = DateTime.UtcNow;
        
        payment.Amount = request.Amount;
        payment.PaymentDate = request.PaymentDate;
        payment.PaidById = request.PaidById ?? userId;
        payment.Notes = request.Notes ?? string.Empty;
        
        await _context.SaveChangesAsync();
        
        return NoContent();
    }
    
    // DELETE: api/debtpayments/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDebtPayment(int id)
    {
        var userId = GetCurrentUserId();
        
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var payment = await _context.DebtPayments
            .Include(dp => dp.Debt)
            .FirstOrDefaultAsync(dp => dp.Id == id && dp.Debt.HouseholdId == user.HouseholdId);
        
        if (payment == null)
        {
            return NotFound(new { message = "Debt payment not found" });
        }
        
        // Restore debt balance
        var debt = payment.Debt;
        debt.CurrentBalance += payment.Amount;
        debt.UpdatedAt = DateTime.UtcNow;
        
        _context.DebtPayments.Remove(payment);
        await _context.SaveChangesAsync();
        
        return NoContent();
    }
}

public class CreateDebtPaymentRequest
{
    public int DebtId { get; init; }
    public decimal Amount { get; init; }
    public DateTime PaymentDate { get; init; }
    public int? PaidById { get; init; }
    public string? Notes { get; init; }
}