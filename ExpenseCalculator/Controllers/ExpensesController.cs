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
    
    // GET: api/expenses/summary
    [HttpGet("summary")]
    public async Task<ActionResult> GetSummary([FromQuery] int householdId = 1)
    {
        var expenses = await _context.Expenses
            .Where(e => e.HouseholdId == householdId)
            .Include(e => e.Category)
            .ToListAsync();
        
        var totalSpent = expenses.Sum(e => e.Amount);

        var byCategory = expenses
            .GroupBy(e => e.Category.Name)
            .Select(g => new
            {
                Category = g.Key,
                Total = g.Sum(e => e.Amount),
                Count = g.Count()
            })
            .OrderByDescending(x => x.Total)
            .ToList();

        var thisMonth = expenses
            .Where(e => e.ExpenseDate.Month == DateTime.UtcNow.Month
                                && e.ExpenseDate.Year == DateTime.UtcNow.Year)
            .Sum(e => e.Amount);

        return Ok(new
        {
            TotalSpent = totalSpent,
            ThisMonth = thisMonth,
            ByCategory = byCategory,
            TotalExpenses = expenses.Count
        });
    }
    
    // POST: api/Expenses
    [HttpPost]
    public async Task<ActionResult<Expense>> CreateExpense(Expense expense)
    {
        // Set Timestamps
        expense.CreatedAt = DateTime.UtcNow;
        expense.UpdatedAt = DateTime.UtcNow;
        
        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetExpense), new { id = expense.Id }, expense);
    }
}