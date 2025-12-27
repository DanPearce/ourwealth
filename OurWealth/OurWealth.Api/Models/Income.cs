namespace OurWealth.Api.Models;

public class Income
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int HouseholdId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal Amount { get; set; }
    public string Source { get; set; }
    public DateTime? RecievedDate { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public User User { get; set; }
    public Household Household { get; set; }
}