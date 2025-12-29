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
public class SettlementsController : ControllerBase
{
    private readonly AppDbContext _context;
    
    public SettlementsController(AppDbContext context)
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
    
    // GET: api/settlements
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Settlement>>> GetSettlements()
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var settlements = await _context.Settlements
            .Where(s => s.HouseholdId == user.HouseholdId)
            .Include(s => s.FromUser)
            .Include(s => s.ToUser)
            .OrderByDescending(s => s.SettlementDate)
            .ToListAsync();
        
        return Ok(settlements);
    }
    
    // GET: api/settlements/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Settlement>> GetSettlement(int id)
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var settlement = await _context.Settlements
            .Include(s => s.FromUser)
            .Include(s => s.ToUser)
            .FirstOrDefaultAsync(s => s.Id == id && s.HouseholdId == user.HouseholdId);
        
        if (settlement == null)
        {
            return NotFound(new { message = "Settlement not found" });
        }
        
        return Ok(settlement);
    }
    
    // POST: api/settlements
    [HttpPost]
    public async Task<ActionResult<Settlement>> CreateSettlement([FromBody] CreateSettlementRequest request)
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        // Verify both users are in the same household
        var fromUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.FromUserId);
        var toUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.ToUserId);
        
        if (fromUser?.HouseholdId != user.HouseholdId || toUser?.HouseholdId != user.HouseholdId)
        {
            return BadRequest(new { message = "Both users must be in your household" });
        }
        
        if (request.FromUserId == request.ToUserId)
        {
            return BadRequest(new { message = "Cannot settle with yourself" });
        }
        
        var settlement = new Settlement
        {
            HouseholdId = user.HouseholdId.Value,
            FromUserId = request.FromUserId,
            ToUserId = request.ToUserId,
            Amount = request.Amount,
            SettlementDate = request.SettlementDate,
            Notes = request.Notes ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.Settlements.Add(settlement);
        await _context.SaveChangesAsync();
        
        await _context.Entry(settlement).Reference(s => s.FromUser).LoadAsync();
        await _context.Entry(settlement).Reference(s => s.ToUser).LoadAsync();
        
        return CreatedAtAction(nameof(GetSettlement), new { id = settlement.Id }, settlement);
    }
    
    // PUT: api/settlements/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSettlement(int id, [FromBody] CreateSettlementRequest request)
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var settlement = await _context.Settlements
            .FirstOrDefaultAsync(s => s.Id == id && s.HouseholdId == user.HouseholdId);
        
        if (settlement == null)
        {
            return NotFound(new { message = "Settlement not found" });
        }
        
        // Verify users are in household
        var fromUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.FromUserId);
        var toUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.ToUserId);
        
        if (fromUser?.HouseholdId != user.HouseholdId || toUser?.HouseholdId != user.HouseholdId)
        {
            return BadRequest(new { message = "Both users must be in your household" });
        }
        
        if (request.FromUserId == request.ToUserId)
        {
            return BadRequest(new { message = "Cannot settle with yourself" });
        }
        
        settlement.FromUserId = request.FromUserId;
        settlement.ToUserId = request.ToUserId;
        settlement.Amount = request.Amount;
        settlement.SettlementDate = request.SettlementDate;
        settlement.Notes = request.Notes ?? string.Empty;
        
        await _context.SaveChangesAsync();
        return NoContent();
    }
    
    // DELETE: api/settlements/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSettlement(int id)
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var settlement = await _context.Settlements
            .FirstOrDefaultAsync(s => s.Id == id && s.HouseholdId == user.HouseholdId);
        
        if (settlement == null)
        {
            return NotFound(new { message = "Settlement not found" });
        }
        
        _context.Settlements.Remove(settlement);
        await _context.SaveChangesAsync();
        
        return NoContent();
    }
    
    // GET: api/settlements/balance
    [HttpGet("balance")]
    public async Task<ActionResult<object>> GetBalance()
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var settlements = await _context.Settlements
            .Where(s => s.HouseholdId == user.HouseholdId)
            .ToListAsync();
        
        // Calculate net balance
        var owedToMe = settlements.Where(s => s.ToUserId == userId).Sum(s => s.Amount);
        var iOwe = settlements.Where(s => s.FromUserId == userId).Sum(s => s.Amount);
        var netBalance = owedToMe - iOwe;
        
        return Ok(new
        {
            userId = userId,
            owedToMe = owedToMe,
            iOwe = iOwe,
            netBalance = netBalance,
            status = netBalance > 0 ? "You are owed" : netBalance < 0 ? "You owe" : "All settled"
        });
    }
}

public class CreateSettlementRequest
{
    public int FromUserId { get; init; }
    public int ToUserId { get; init; }
    public decimal Amount { get; init; }
    public DateTime SettlementDate { get; init; }
    public string? Notes { get; init; }
}