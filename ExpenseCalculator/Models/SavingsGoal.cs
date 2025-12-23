namespace ExpenseCalculator.Models;

public class SavingsGoal
{
    public int Id { get; set; }
    public int HouseholdId { get; set; }
    public string Name { get; set; }
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public DateTime? TargetDate { get; set; }
    public string Priority { get; set; }
    public bool IsActive { get; set; }
    public string Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public Household Household { get; set; }
    public ICollection<SavingsContribution> SavingContributions { get; set; }
}