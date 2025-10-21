namespace TravelHub.Domain.Entities;

public class Day
{
    public int Id { get; set; }
    public int? Number { get; set; } // The number of the day in the trip, e.g., Day 1
    public string? Name { get; set; }
    public DateTime Date { get; set; }

    // Foreign Key for Trip
    public int TripId { get; set; }
    // Navigation Property back to the trip (1:N)
    public Trip? Trip { get; set; }

    // A day can have multiple activities (1:N)
    public ICollection<Activity> Activities { get; set; } = new List<Activity>();
}
