namespace TravelHub.Domain.Entities;

public class Expense
{
    public int Id { get; set; }
    public decimal Value { get; set; }
    public decimal EstimatedValue { get; set; }
    public required string Name { get; set; }
    public bool IsEstimated { get; set; }

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
    public int ExchangeRateId { get; set; }
    // Navigation Property to the currency
    public ExchangeRate? ExchangeRate { get; set; }

    // Foreign Key for Spot
    public int? SpotId { get; set; }
    // Navigation Property to the spot
    public Spot? Spot { get; set; }

    // Foreign Key for Transport
    public int? TransportId { get; set; }
    // Navigation Property to the transport
    public Transport? Transport { get; set; }

    // Navigation property for people the expense was for (M:N)
    // public ICollection<Person> Participants { get; set; } = new List<Person>();
    public ICollection<ExpenseParticipant> Participants { get; set; } = new List<ExpenseParticipant>();
}
