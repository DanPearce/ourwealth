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
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _context;
    
    public CategoriesController(AppDbContext context)
    {
        _context = context;
    }
    
    // Helper method to get current user's ID from JWT token
    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return int.Parse(userIdClaim.Value);
    }
    
    // GET: api/categories
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
    {
        var userId = GetCurrentUserId();
        
        // Get user's household
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var categories = await _context.Categories
            .Where(c => c.HouseholdId == user.HouseholdId && c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
        
        return Ok(categories);
    }
    
    // GET: api/categories/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Category>> GetCategory(int id)
    {
        var userId = GetCurrentUserId();
        
        // Get user's household
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.HouseholdId == user.HouseholdId);
        
        if (category == null)
        {
            return NotFound(new { message = "Category not found" });
        }
        
        return Ok(category);
    }
    
    // POST: api/categories
    [HttpPost]
    public async Task<ActionResult<Category>> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        var userId = GetCurrentUserId();
        
        // Get user's household
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household to create categories" });
        }
        
        var category = new Category
        {
            HouseholdId = user.HouseholdId.Value,
            Name = request.Name,
            ParentCategoryId = request.ParentCategoryId,
            Priority = request.Priority,
            Icon = request.Icon,
            Color = request.Color,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        
        return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
    }
    
    // PUT: api/categories/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] CreateCategoryRequest request)
    {
        var userId = GetCurrentUserId();
        
        // Get user's household
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.HouseholdId == user.HouseholdId);
        
        if (category == null)
        {
            return NotFound(new { message = "Category not found" });
        }
        
        // Update fields
        category.Name = request.Name;
        category.ParentCategoryId = request.ParentCategoryId;
        category.Priority = request.Priority;
        category.Icon = request.Icon;
        category.Color = request.Color;
        
        await _context.SaveChangesAsync();
        
        return NoContent();
    }
    
    // DELETE: api/categories/5 (soft delete)
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var userId = GetCurrentUserId();
        
        // Get user's household
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user?.HouseholdId == null)
        {
            return BadRequest(new { message = "User must be part of a household" });
        }
        
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.HouseholdId == user.HouseholdId);
        
        if (category == null)
        {
            return NotFound(new { message = "Category not found" });
        }
        
        // Soft delete - mark as inactive instead of removing
        category.IsActive = false;
        await _context.SaveChangesAsync();
        
        return NoContent();
    }
}

// DTO for creating/updating categories
public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public int? ParentCategoryId { get; set; }
    public string Priority { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}