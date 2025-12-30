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
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;
    
    public UsersController(AppDbContext context)
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
    
    // GET: api/users/profile
    [HttpGet("profile")]
    public async Task<ActionResult<UserProfileResponse>> GetProfile()
    {
        var userId = GetCurrentUserId();
        
        var user = await _context.Users
            .Include(u => u.Household)
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }
        
        var response = new UserProfileResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Household = user.Household != null ? new HouseholdInfo
            {
                Id = user.Household.Id,
                Name = user.Household.Name,
                UseJointAccount = user.Household.UseJointAccount,
                Currency = user.Household.Currency
            } : null
        };
        
        return Ok(response);
    }
    
    // GET: api/users/household-members
    [HttpGet("household-members")]
    public async Task<ActionResult<List<HouseholdMemberResponse>>> GetHouseholdMembers()
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var members = await _context.Users
            .Where(u => u.HouseholdId == user.HouseholdId)
            .Select(u => new HouseholdMemberResponse
            {
                Id = u.Id,
                Username = u.Username,
                DisplayName = u.DisplayName,
                Email = u.Email
            })
            .ToListAsync();
        
        return Ok(members);
    }
}

// Response DTOs
public class UserProfileResponse
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public HouseholdInfo? Household { get; set; }
}

public class HouseholdInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool UseJointAccount { get; set; }
    public string Currency { get; set; } = string.Empty;
}

public class HouseholdMemberResponse
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}