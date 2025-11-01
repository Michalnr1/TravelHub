namespace TravelHub.Domain.Entities;

public class Trip
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public Status Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsPrivate { get; set; }
    public CurrencyCode CurrencyCode { get; set; }

    // Foreign Key for Person
    public required string PersonId { get; set; }
    // Navigation Property for the person who owns the trip (1:N)
    public Person? Person { get; set; }

    // A trip consists of multiple days (1:N)
    public ICollection<Day> Days { get; set; } = new List<Day>();

    // A trip can have many activities (1:N)
    public ICollection<Activity> Activities { get; set; } = new List<Activity>();

    // A trip can have multiple transport legs (1:N)
    public ICollection<Transport> Transports { get; set; } = new List<Transport>();

    // A trip can have many expenses (1:N)
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();

    // A trip can have many exchange rates (1:N)
    public ICollection<ExchangeRate> ExchangeRates { get; set; } = new List<ExchangeRate>();

    // A trip can have many countries (1:N)
    public ICollection<Country> Countries { get; set; } = new List<Country>();
}
