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
public class SavingsContributionsController : ControllerBase
{
    private readonly AppDbContext _context;
    
    public SavingsContributionsController(AppDbContext context)
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
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SavingsContribution>>> GetSavingsContributions([FromQuery] int? savingsGoalId)
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var query = _context.SavingsContributions
            .Include(sc => sc.SavingsGoal)
            .Where(sc => sc.SavingsGoal.HouseholdId == user.HouseholdId);
        
        if (savingsGoalId.HasValue)
        {
            query = query.Where(sc => sc.SavingsGoalId == savingsGoalId.Value);
        }
        
        var contributions = await query.OrderByDescending(sc => sc.ContributionDate).ToListAsync();
        return Ok(contributions);
    }
    
    [HttpPost]
    public async Task<ActionResult<SavingsContribution>> CreateSavingsContribution([FromBody] CreateSavingsContributionRequest request)
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var goal = await _context.SavingsGoals
            .FirstOrDefaultAsync(sg => sg.Id == request.SavingsGoalId && sg.HouseholdId == user.HouseholdId);
        
        if (goal == null)
        {
            return BadRequest(new { message = "Savings goal not found" });
        }
        
        var contribution = new SavingsContribution
        {
            SavingsGoalId = request.SavingsGoalId,
            Amount = request.Amount,
            ContributionDate = request.ContributionDate,
            Notes = request.Notes ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.SavingsContributions.Add(contribution);
        
        // Update goal current amount
        goal.CurrentAmount += request.Amount;
        goal.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        await _context.Entry(contribution).Reference(sc => sc.SavingsGoal).LoadAsync();
        
        return CreatedAtAction(nameof(GetSavingsContributions), new { id = contribution.Id }, contribution);
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSavingsContribution(int id)
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var contribution = await _context.SavingsContributions
            .Include(sc => sc.SavingsGoal)
            .FirstOrDefaultAsync(sc => sc.Id == id && sc.SavingsGoal.HouseholdId == user.HouseholdId);
        
        if (contribution == null)
        {
            return NotFound(new { message = "Contribution not found" });
        }
        
        // Restore goal amount
        var goal = contribution.SavingsGoal;
        goal.CurrentAmount -= contribution.Amount;
        goal.UpdatedAt = DateTime.UtcNow;
        
        _context.SavingsContributions.Remove(contribution);
        await _context.SaveChangesAsync();
        
        return NoContent();
    }
}

public class CreateSavingsContributionRequest
{
    public int SavingsGoalId { get; init; }
    public decimal Amount { get; init; }
    public DateTime ContributionDate { get; init; }
    public string? Notes { get; init; }
}