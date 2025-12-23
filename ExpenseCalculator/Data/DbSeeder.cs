using ExpenseCalculator.Models;

namespace ExpenseCalculator.Data
{
    public static class DbSeeder
    {
        public static void SeedData(AppDbContext context)
        {
            // Check if data already exists
            if (context.Households.Any())
            {
                return; // Data already seeded
            }
            
            // Create Household
            var household = new Household
            {
                Name = "PearceGoodwin",
                UseJointAccount = true,
                Currency = "GBP",
                CreatedAt = DateTime.UtcNow,
            };
            context.Households.Add(household);
            context.SaveChanges();
            
            //Create Users
            var dan = new User
            {
                Username = "Dan",
                Email = "dan@example.com",
                PasswordHash = "temp",
                DisplayName = "Dan",
                HouseholdId = household.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            var harry = new User
            {
                Username = "Harry",
                Email = "harry@example.com",
                PasswordHash = "temp",
                DisplayName = "Harry",
                HouseholdId = household.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            context.Users.AddRange(dan, harry);
            context.SaveChanges();
            
            // Create Categories
            var categories = new[]
            {
                new Category { HouseholdId = household.Id, Name = "Housing", Priority = "Essential", Icon = "home", Color = "#3B82F6", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Category { HouseholdId = household.Id, Name = "Food", Priority = "Essential", Icon = "utensils", Color = "#10B981", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Category { HouseholdId = household.Id, Name = "Transport", Priority = "Important", Icon = "car", Color = "#F59E0B", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Category { HouseholdId = household.Id, Name = "Utilities", Priority = "Essential", Icon = "bolt", Color = "#EF4444", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Category { HouseholdId = household.Id, Name = "Entertainment", Priority = "Optional", Icon = "tv", Color = "#8B5CF6", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Category { HouseholdId = household.Id, Name = "Healthcare", Priority = "Important", Icon = "heart", Color = "#EC4899", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Category { HouseholdId = household.Id, Name = "Shopping", Priority = "Optional", Icon = "shopping-bag", Color = "#6366F1", IsActive = true, CreatedAt = DateTime.UtcNow },
            };
            
            context.Categories.AddRange(categories);
            context.SaveChanges();

            var expenses = new[]
            {
                new Expense { HouseholdId = household.Id, CategoryId = categories[1].Id, Description = "Sainsbury's Grocery", Amount = 45.60m, ExpenseDate = DateTime.UtcNow.AddDays(-2), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Expense { HouseholdId = household.Id, CategoryId = categories[4].Id, Description = "Cinema tickets", Amount = 24.00m, ExpenseDate = DateTime.UtcNow.AddDays(-5), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Expense { HouseholdId = household.Id, CategoryId = categories[1].Id, Description = "Restaurant", Amount = 68.00m, ExpenseDate = DateTime.UtcNow.AddDays(-1), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            };
            
            context.Expenses.AddRange(expenses);
            context.SaveChanges();
        }
    }
}