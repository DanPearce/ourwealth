using Microsoft.EntityFrameworkCore;
using OurWealth.Api.Models;

namespace OurWealth.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    
    // DbSets for each model
    public DbSet<User> Users { get; set; }
    public DbSet<Household> Households { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Budget> Budgets { get; set; }
    public DbSet<Income> Incomes { get; set; }
    public DbSet<RecurringBill> RecurringBills { get; set; }
    public DbSet<BillPayment> BillPayments { get; set; }
    public DbSet<Expense> Expenses { get; set; }
    public DbSet<SavingsGoal> SavingsGoals { get; set; }
    public DbSet<SavingsContribution> SavingsContributions { get; set; }
    public DbSet<Debt> Debts { get; set; }
    public DbSet<DebtPayment> DebtPayments { get; set; }
    public DbSet<Settlement> Settlements { get; set; }
}