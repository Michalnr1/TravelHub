namespace TravelHub.Domain.Entities;

public class Trip
{
    public int Id { get; set; }
    public string Name { get; set; }
    public Status Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Foreign Key for Person
    public string PersonId { get; set; }
    // Navigation Property for the person who owns the trip (1:N)
    public Person Person { get; set; }

    // A trip consists of multiple days (1:N)
    public ICollection<Day> Days { get; set; } = new List<Day>();

    // A trip can have multiple transport legs (1:N)
    public ICollection<Transport> Transports { get; set; } = new List<Transport>();
}
