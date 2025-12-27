using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OurWealth.Api.Models;
using OurWealth.Api.Data;

namespace OurWealth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]

public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _context;
    
    public CategoriesController(AppDbContext context)
    {
        _context = context;
    }
    
    // GET: api/Categories
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
    {
        var categories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
        return Ok(categories);
    }
}