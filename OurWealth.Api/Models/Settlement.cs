namespace OurWealth.Api.Models;

public class Settlement
{
    public int Id { get; set; }
    public int HouseholdId { get; set; }
    public int FromUserId { get; set; }
    public int ToUserId { get; set; }
    public decimal Amount { get; set; }
    public DateTime SettlementDate { get; set; }
    public string Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public Household Household { get; set; }
    public User FromUser { get; set; }
    public User ToUser { get; set; }
}