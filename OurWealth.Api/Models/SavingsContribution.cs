namespace OurWealth.Api.Models;

public class SavingsContribution
{
    public int Id { get; set; }
    public int SavingsGoalId { get; set; }
    public decimal Amount { get; set; }
    public DateTime ContributionDate { get; set; }
    public string Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public SavingsGoal SavingsGoal { get; set; }
    public User ContributedBy { get; set; }
}