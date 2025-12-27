namespace OurWealth.Api.Models;

public class Expense
{
    public int Id { get; set; }
    public int HouseholdId { get; set; }
    public int CategoryId { get; set; }
    public string Description { get; set; }
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public int? PaidByUserId { get; set; }
    public string Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public Household Household { get; set; }
    public Category Category { get; set; }
    public User PaidByUser { get; set; }
}