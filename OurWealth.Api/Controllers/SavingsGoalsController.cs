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
public class SavingsGoalsController : ControllerBase
{
    private readonly AppDbContext _context;
    
    public SavingsGoalsController(AppDbContext context)
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
    public async Task<ActionResult<IEnumerable<SavingsGoal>>> GetSavingsGoals([FromQuery] bool? isActive)
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var query = _context.SavingsGoals.Where(sg => sg.HouseholdId == user.HouseholdId);
        
        if (isActive.HasValue)
        {
            query = query.Where(sg => sg.IsActive == isActive.Value);
        }
        
        var goals = await query.OrderByDescending(sg => sg.Priority).ToListAsync();
        return Ok(goals);
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<SavingsGoal>> GetSavingsGoal(int id)
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var goal = await _context.SavingsGoals
            .Include(sg => sg.SavingContributions)
            .FirstOrDefaultAsync(sg => sg.Id == id && sg.HouseholdId == user.HouseholdId);
        
        if (goal == null)
        {
            return NotFound(new { message = "Savings goal not found" });
        }
        
        return Ok(goal);
    }
    
    [HttpPost]
    public async Task<ActionResult<SavingsGoal>> CreateSavingsGoal([FromBody] CreateSavingsGoalRequest request)
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var goal = new SavingsGoal
        {
            HouseholdId = user.HouseholdId.Value,
            Name = request.Name,
            TargetAmount = request.TargetAmount,
            CurrentAmount = 0,
            TargetDate = request.TargetDate,
            Priority = request.Priority,
            IsActive = true,
            Notes = request.Notes ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _context.SavingsGoals.Add(goal);
        await _context.SaveChangesAsync();
        
        return CreatedAtAction(nameof(GetSavingsGoal), new { id = goal.Id }, goal);
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSavingsGoal(int id, [FromBody] CreateSavingsGoalRequest request)
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var goal = await _context.SavingsGoals
            .FirstOrDefaultAsync(sg => sg.Id == id && sg.HouseholdId == user.HouseholdId);
        
        if (goal == null)
        {
            return NotFound(new { message = "Savings goal not found" });
        }
        
        goal.Name = request.Name;
        goal.TargetAmount = request.TargetAmount;
        goal.TargetDate = request.TargetDate;
        goal.Priority = request.Priority;
        goal.Notes = request.Notes ?? string.Empty;
        goal.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return NoContent();
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSavingsGoal(int id)
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var goal = await _context.SavingsGoals
            .FirstOrDefaultAsync(sg => sg.Id == id && sg.HouseholdId == user.HouseholdId);
        
        if (goal == null)
        {
            return NotFound(new { message = "Savings goal not found" });
        }
        
        goal.IsActive = false;
        goal.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        
        return NoContent();
    }
}

public class CreateSavingsGoalRequest
{
    public string Name { get; init; } = string.Empty;
    public decimal TargetAmount { get; init; }
    public DateTime? TargetDate { get; init; }
    public string Priority { get; init; } = string.Empty;
    public string? Notes { get; init; }
}