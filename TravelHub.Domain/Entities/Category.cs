namespace TravelHub.Domain.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Color { get; set; } // e.g., Hex code like "#FF5733"

    // A category can have many expenses
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
