using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExpenseCalculator.Models;
using ExpenseCalculator.Data;

namespace ExpenseCalculator.Controllers;

[ApiController]
[Route("api/[controller]")]

public class ExpensesController : ControllerBase
{
    private readonly AppDbContext _context;
    
    public ExpensesController(AppDbContext context)
    {
        _context = context;
    }
    
    // GET: api/Expenses
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Expense>>> GetExpenses()
    {
        var expenses = await _context.Expenses
            .Include(e => e.Category)
            .Include(e => e.Household)
            .OrderByDescending(e => e.ExpenseDate)
            .ToListAsync();
        
        return Ok(expenses);
    }
    
    // GET: api/Expenses/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Expense>> GetExpense(int id)
    {
        var expense = await _context.Expenses
            .Include(e => e.Category)
            .Include(e => e.Household)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (expense == null)
        {
            return NotFound();
        }
        return Ok(expense);
    }
}