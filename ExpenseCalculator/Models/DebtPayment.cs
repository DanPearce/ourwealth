namespace ExpenseCalculator.Models;

public class DebtPayment
{
    public int Id { get; set; }
    public int DebtId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public int? PaidById { get; set; }
    public string Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public Debt Debt { get; set; }
    public User PaidBy { get; set; }
}