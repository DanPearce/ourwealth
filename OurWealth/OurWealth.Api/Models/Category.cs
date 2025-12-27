namespace OurWealth.Api.Models;

public class Category
{
    public int Id { get; set; }
    public int HouseholdId { get; set; }
    public string Name { get; set; }
    public int? ParentCategoryId { get; set; }
    public string Priority { get; set; }
    public string Icon { get; set; }
    public string Color { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public Household Household { get; set; }
    public Category ParentCategory { get; set; }
    public ICollection<Category> SubCategories { get; set; }
    public ICollection<Expense> Expenses { get; set; }
}