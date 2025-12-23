namespace ExpenseCalculator.Models;

public class Budget
{
    public int Id { get; set; }
    public int HouseholdId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public int? CategoryId { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    
    // Navigation properties
    public Household Household { get; set; }
    public Category Category { get; set; }
}