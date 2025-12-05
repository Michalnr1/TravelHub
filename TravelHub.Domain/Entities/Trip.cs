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
    public string? Catalog { get; set; }

    // Checklist as a JSON
    public Checklist? Checklist { get; set; } = new();

    // Foreign Key for Person
    public required string PersonId { get; set; }
    // Navigation Property for the person who owns the trip (1:N)
    public Person? Person { get; set; }

    // Foreign Key for Blog
    public int? BlogId { get; set; }
    // Navigation Property to the blog
    public Blog? Blog { get; set; }

    // A trip can have many participants (M:N through TripParticipant)
    public ICollection<TripParticipant> Participants { get; set; } = new List<TripParticipant>();

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

    // A trip can have many chat messages (1:N)
    public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

    // A trip can have many flight infos (1:N)
    public ICollection<FlightInfo> FlightInfos { get; set; } = new List<FlightInfo>();
}
