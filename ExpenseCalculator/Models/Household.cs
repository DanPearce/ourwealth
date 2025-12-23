using System.Collections.Generic;

namespace ExpenseCalculator.Models
{
    // Household class - represents a household with members and expenses
    public class Household
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool UseJointAccount { get; set; }
        public string Currency { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // Navigation properties
        public ICollection<User> Users { get; set; }
        public ICollection<Category> Categories { get; set; }
        public ICollection<Expense> Expenses { get; set; }
        public ICollection<RecurringBill> RecurringBills { get; set; }
        public ICollection<Budget> Budgets { get; set; }
        public ICollection<Income> Income { get; set; }
        public ICollection<SavingsGoal> SavingsGoals { get; set; }
        public ICollection<Debt> Debt { get; set; }
    }
}

