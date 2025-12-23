namespace ExpenseCalculator.Models;

public class BillPayment
{
    public int Id { get; set; }
    public int RecurringBillId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaidDate { get; set; }
    public int? PaidByUserId { get; set; }
    public string Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public RecurringBill RecurringBill { get; set; }
    public User PaidByUser { get; set; }
}