namespace TravelHub.Domain.Entities;

public class Activity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Duration { get; set; }
    public int Order { get; set; } // The order of the activity within the day

    // Foreign Key for Day
    public int DayId { get; set; }
    // Navigation Property back to the day (1:N)
    public Day Day { get; set; }

    // An activity can occur at multiple spots (1:N)
    public ICollection<Spot> Spots { get; set; } = new List<Spot>();
}
