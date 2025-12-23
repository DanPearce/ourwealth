namespace ExpenseCalculator.Models;

public class Debt
{
    public int Id { get; set; }
    public int HouseholdId { get; set; }
    public string Name { get; set; }
    public string DebtType { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal? InterestRate { get; set; }
    public decimal? MinimumPayment { get; set; }
    public int? PaymentDayOfMonth { get; set; }
    public string Creditor { get; set; }
    public bool IsActive { get; set; }
    public string Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public Household Household { get; set; }
    public ICollection<DebtPayment> DebtPayments { get; set; }
}