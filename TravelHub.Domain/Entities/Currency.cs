using System.ComponentModel.DataAnnotations;

namespace TravelHub.Domain.Entities;

public class Currency
{
    [Key] // The currency code (e.g., "USD", "EUR") is the natural primary key
    public required string Key { get; set; }
    public required string Name { get; set; } // e.g., "United States Dollar"
    public decimal ExchangeRate { get; set; }

    // A currency can be used in many expenses
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
