namespace TravelHub.Domain.Entities;

public class Activity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public decimal Duration { get; set; }
    public int Order { get; set; } // The order of the activity within the day
    public decimal? StartTime { get; set; }

    // Checklist as a JSON
    public Checklist? Checklist { get; set; } = new();

    // Foreign Key for Category (N:1)
    public int? CategoryId { get; set; }
    // Navigation Property back to the Category
    public Category? Category { get; set; }

    // Foreign Key for Trip
    public int TripId { get; set; }
    // Navigation Property back to the trip (1:N)
    public Trip? Trip { get; set; }

    // Foreign Key for Day
    public int? DayId { get; set; }
    // Navigation Property back to the day (1:N)
    public Day? Day { get; set; }
}
