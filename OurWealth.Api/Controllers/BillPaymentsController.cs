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
public class BillPaymentsController : ControllerBase
{
    private readonly AppDbContext _context;
    
    public BillPaymentsController(AppDbContext context)
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
    // GET: api/billpayments
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BillPayment>>> GetBillPayments([FromQuery] int? recurringBillId, [FromQuery] int? month, [FromQuery] int? year)
    {
        var userId = GetCurrentUserId();
    
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
    
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
    
        // Join with RecurringBills to filter by household
        var query = _context.BillPayments
            .Include(bp => bp.RecurringBill)
            .ThenInclude(rb => rb.Category)
            .Include(bp => bp.PaidByUser)
            .Where(bp => bp.RecurringBill.HouseholdId == user.HouseholdId);
    
        // Optional filters
        if (recurringBillId.HasValue)
        {
            query = query.Where(bp => bp.RecurringBillId == recurringBillId.Value);
        }
    
        if (month.HasValue)
        {
            query = query.Where(bp => bp.Month == month.Value);
        }
    
        if (year.HasValue)
        {
            query = query.Where(bp => bp.Year == year.Value);
        }
    
        var payments = await query
            .OrderByDescending(bp => bp.Year)
            .ThenByDescending(bp => bp.Month)
            .ToListAsync();
    
        return Ok(payments);
    }
    // GET: api/billpayments/5
    [HttpGet("{id}")]
    public async Task<ActionResult<BillPayment>> GetBillPayment(int id)
    {
        var userId = GetCurrentUserId();
        
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var payment = await _context.BillPayments
            .Include(bp => bp.RecurringBill)
            .ThenInclude(rb => rb.Category)
            .Include(bp => bp.PaidByUser)
            .FirstOrDefaultAsync(bp => bp.Id == id && bp.RecurringBill.HouseholdId == user.HouseholdId);
        
        if (payment == null)
        {
            return NotFound(new { message = "Bill payment not found" });
        }
        
        return Ok(payment);
    }

    // POST: api/billpayments
    [HttpPost]
    public async Task<ActionResult<BillPayment>> CreateBillPayment([FromBody] CreateBillPaymentRequest request)
    {
        var userId = GetCurrentUserId();
        
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household to create bill payments" });
        }
        
        // Verify the recurring bill belongs to this household
        var recurringBill = await _context.RecurringBills
            .FirstOrDefaultAsync(rb => rb.Id == request.RecurringBillId && rb.HouseholdId == user.HouseholdId);
        
        if (recurringBill == null)
        {
            return BadRequest(new { message = "Recurring bill not found in your household" });
        }
        
        var payment = new BillPayment
        {
            RecurringBillId = request.RecurringBillId,
            Month = request.Month,
            Year = request.Year,
            Amount = request.Amount,
            PaidDate = request.PaidDate,
            PaidByUserId = request.PaidByUserId ?? userId,  // Default to current user
            Notes = request.Notes ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.BillPayments.Add(payment);
        await _context.SaveChangesAsync();
        
        await _context.Entry(payment).Reference(bp => bp.RecurringBill).LoadAsync();
        await _context.Entry(payment.RecurringBill).Reference(rb => rb.Category).LoadAsync();
        if (payment.PaidByUserId.HasValue)
        {
            await _context.Entry(payment).Reference(bp => bp.PaidByUser).LoadAsync();
        }
        
        return CreatedAtAction(nameof(GetBillPayment), new { id = payment.Id }, payment);
    }

    // PUT: api/billpayments/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBillPayment(int id, [FromBody] CreateBillPaymentRequest request)
    {
        var userId = GetCurrentUserId();
        
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var payment = await _context.BillPayments
            .Include(bp => bp.RecurringBill)
            .FirstOrDefaultAsync(bp => bp.Id == id && bp.RecurringBill.HouseholdId == user.HouseholdId);
        
        if (payment == null)
        {
            return NotFound(new { message = "Bill payment not found" });
        }
        
        payment.Month = request.Month;
        payment.Year = request.Year;
        payment.Amount = request.Amount;
        payment.PaidDate = request.PaidDate;
        payment.PaidByUserId = request.PaidByUserId ?? userId;
        payment.Notes = request.Notes ?? string.Empty;
        
        await _context.SaveChangesAsync();
        
        return NoContent();
    }

    // DELETE: api/billpayments/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBillPayment(int id)
    {
        var userId = GetCurrentUserId();
        
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var payment = await _context.BillPayments
            .Include(bp => bp.RecurringBill)
            .FirstOrDefaultAsync(bp => bp.Id == id && bp.RecurringBill.HouseholdId == user.HouseholdId);
        
        if (payment == null)
        {
            return NotFound(new { message = "Bill payment not found" });
        }
        
        _context.BillPayments.Remove(payment);  // Hard delete for payments
        await _context.SaveChangesAsync();
        
        return NoContent();
    }
    
    // DTO for creating/updating bill payments
    public class CreateBillPaymentRequest
    {
        public int RecurringBillId { get; init; }
        public int Month { get; init; }
        public int Year { get; init; }
        public decimal Amount { get; init; }
        public DateTime PaidDate { get; init; }
        public int? PaidByUserId { get; init; }
        public string? Notes { get; init; }
    }
}