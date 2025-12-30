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
    
    // GET: api/recurringbills/upcoming?days=7
    [HttpGet("upcoming")]
    public async Task<ActionResult<List<UpcomingBillResponse>>> GetUpcomingBills(
        [FromQuery] int days = 30)
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var householdId = user.HouseholdId.Value;
        var today = DateTime.UtcNow.Date;
        var currentMonth = today.Month;
        var currentYear = today.Year;
        var currentDay = today.Day;
        
        var recurringBills = await _context.RecurringBills
            .Where(rb => rb.HouseholdId == householdId && rb.IsActive)
            .Include(rb => rb.Category)
            .ToListAsync();
        
        var upcomingBills = new List<UpcomingBillResponse>();
        
        foreach (var bill in recurringBills)
        {
            if (!bill.DayOfMonth.HasValue || bill.DayOfMonth.Value < 1 || bill.DayOfMonth.Value > 31)
            {
                continue;
            }

            var dueDate = new DateTime(currentYear, currentMonth, bill.DayOfMonth.Value);           
            
            if (dueDate < today)
            {
                dueDate = dueDate.AddMonths(1);
            }
            
            var daysUntilDue = (dueDate - today).Days;
            
            if (daysUntilDue <= days)
            {
                var isPaid = await _context.BillPayments
                    .AnyAsync(bp => bp.RecurringBillId == bill.Id
                                 && bp.Month == dueDate.Month
                                 && bp.Year == dueDate.Year);
                
                if (!isPaid)
                {
                    upcomingBills.Add(new UpcomingBillResponse
                    {
                        BillId = bill.Id,
                        Description = bill.Description,
                        Amount = bill.Amount,
                        IsVariableAmount = bill.IsVariableAmount,
                        DueDate = dueDate,
                        DaysUntilDue = daysUntilDue,
                        IsOverdue = daysUntilDue < 0,
                        CategoryName = bill.Category?.Name ?? "Uncategorized",
                        CategoryColor = bill.Category?.Color ?? "#9CA3AF"
                    });
                }
            }
        }
        return Ok(upcomingBills.OrderBy(b => b.DueDate).ToList());
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
    
    public class UpcomingBillResponse
    {
        public int BillId { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal? Amount { get; set; }
        public bool IsVariableAmount { get; set; }
        public DateTime DueDate { get; set; }
        public int DaysUntilDue { get; set; }
        public bool IsOverdue { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryColor { get; set; } = string.Empty;
    }
}