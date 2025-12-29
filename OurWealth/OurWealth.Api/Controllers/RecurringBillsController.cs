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
public class RecurringBillsController : ControllerBase
{
    private readonly AppDbContext _context;
    
    public RecurringBillsController(AppDbContext context)
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
    // GET: api/recurringbills
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RecurringBill>>> GetRecurringBills([FromQuery] bool? isActive)
    {
        var userId = GetCurrentUserId();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }

        var query = _context.RecurringBills
            .Where(rb => rb.HouseholdId == user.HouseholdId);

        // Optional: filter by active status
        if (isActive.HasValue)
        {
            query = query.Where(rb => rb.IsActive == isActive.Value);
        }

        var bills = await query
            .Include(rb => rb.Category)
            .Include(rb => rb.PaidByUser)
            .OrderBy(rb => rb.DayOfMonth)
            .ToListAsync();

        return Ok(bills);
    }
    // GET: api/recurringbills/5
    [HttpGet("{id}")]
    public async Task<ActionResult<RecurringBill>> GetRecurringBill(int id)
    {
        var userId = GetCurrentUserId();
        
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var bill = await _context.RecurringBills
            .Include(rb => rb.Category)
            .Include(rb => rb.PaidByUser)
            .Include(rb => rb.BillPayments)
            .FirstOrDefaultAsync(rb => rb.Id == id && rb.HouseholdId == user.HouseholdId);
        
        if (bill == null)
        {
            return NotFound(new { message = "Recurring bill not found" });
        }
        
        return Ok(bill);
    }

    // POST: api/recurringbills
    [HttpPost]
    public async Task<ActionResult<RecurringBill>> CreateRecurringBill([FromBody] CreateRecurringBillRequest request)
    {
        var userId = GetCurrentUserId();
        
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household to create recurring bills" });
        }
        
        var bill = new RecurringBill
        {
            HouseholdId = user.HouseholdId.Value,
            CategoryId = request.CategoryId,
            Description = request.Description,
            Amount = request.Amount,
            IsVariableAmount = request.IsVariableAmount,
            Frequency = request.Frequency,
            DayOfMonth = request.DayOfMonth,
            DueDate = request.DueDate,
            ReminderDaysBefore = request.ReminderDaysBefore,
            IsActive = true,
            PaidByUserId = request.PaidByUserId,
            Notes = request.Notes ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.RecurringBills.Add(bill);
        await _context.SaveChangesAsync();
        
        await _context.Entry(bill).Reference(rb => rb.Category).LoadAsync();
        if (bill.PaidByUserId.HasValue)
        {
            await _context.Entry(bill).Reference(rb => rb.PaidByUser).LoadAsync();
        }
        
        return CreatedAtAction(nameof(GetRecurringBill), new { id = bill.Id }, bill);
    }

    // PUT: api/recurringbills/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRecurringBill(int id, [FromBody] CreateRecurringBillRequest request)
    {
        var userId = GetCurrentUserId();
        
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var bill = await _context.RecurringBills
            .FirstOrDefaultAsync(rb => rb.Id == id && rb.HouseholdId == user.HouseholdId);
        
        if (bill == null)
        {
            return NotFound(new { message = "Recurring bill not found" });
        }
        
        bill.CategoryId = request.CategoryId;
        bill.Description = request.Description;
        bill.Amount = request.Amount;
        bill.IsVariableAmount = request.IsVariableAmount;
        bill.Frequency = request.Frequency;
        bill.DayOfMonth = request.DayOfMonth;
        bill.DueDate = request.DueDate;
        bill.ReminderDaysBefore = request.ReminderDaysBefore;
        bill.PaidByUserId = request.PaidByUserId;
        bill.Notes = request.Notes ?? string.Empty;
        
        await _context.SaveChangesAsync();
        
        return NoContent();
    }

    // DELETE: api/recurringbills/5 (soft delete)
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRecurringBill(int id)
    {
        var userId = GetCurrentUserId();
        
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var bill = await _context.RecurringBills
            .FirstOrDefaultAsync(rb => rb.Id == id && rb.HouseholdId == user.HouseholdId);
        
        if (bill == null)
        {
            return NotFound(new { message = "Recurring bill not found" });
        }
        
        bill.IsActive = false;  // Soft delete
        await _context.SaveChangesAsync();
        
        return NoContent();
    }
    
    // DTO for creating/updating recurring bills
    public class CreateRecurringBillRequest
    {
        public int CategoryId { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal? Amount { get; set; }
        public bool IsVariableAmount { get; set; }
        public string Frequency { get; set; } = string.Empty;
        public int? DayOfMonth { get; set; }
        public DateTime? DueDate { get; set; }
        public int ReminderDaysBefore { get; set; }
        public int? PaidByUserId { get; set; }
        public string? Notes { get; set; }
    }
}