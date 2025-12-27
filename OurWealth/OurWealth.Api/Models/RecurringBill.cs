namespace OurWealth.Api.Models;

public class RecurringBill
{
    public int Id { get; set; }
    public int HouseholdId { get; set; }
    public int CategoryId { get; set; }
    public string Description { get; set; }
    public decimal? Amount { get; set; }
    public bool IsVariableAmount { get; set; }
    public string Frequency { get; set; }
    public int? DayOfMonth { get; set; }
    public DateTime? DueDate { get; set; }
    public int ReminderDaysBefore { get; set; }
    public bool IsActive { get; set; }
    public int? PaidByUserId { get; set; }
    public string Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public Household Household { get; set; }
    public Category Category { get; set; }
    public User PaidByUser { get; set; }
    public ICollection<BillPayment> BillPayments { get; set; }
}