namespace TravelHub.Domain.Entities;

public class Category
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Color { get; set; } // e.g., Hex code like "#FF5733"

    // A category can have many Activities
    public ICollection<Activity> Activities { get; set; } = new List<Activity>();

    // A category can have many Expenses
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
