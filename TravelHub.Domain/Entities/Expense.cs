namespace TravelHub.Domain.Entities;

public class Expense
{
    public int Id { get; set; }
    public decimal Value { get; set; }
    public required string Name { get; set; }

    // Foreign Key for the person who paid
    public required string PaidById { get; set; }
    // Navigation Property to the person who paid
    public Person? PaidBy { get; set; }

    // Foreign Key for Category
    public int? CategoryId { get; set; }
    // Navigation Property to the category
    public Category? Category { get; set; }

    // Foreign Key for Trip
    public int TripId { get; set; }
    // Navigation Property to the trip
    public Trip? Trip { get; set; }

    // Foreign Key for Currency
    public required CurrencyCode CurrencyKey { get; set; }
    // Navigation Property to the currency
    public Currency? Currency { get; set; }

    // Navigation property for people the expense was for (M:N)
    public ICollection<Person> Participants { get; set; } = new List<Person>();
}
