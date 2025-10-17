namespace TravelHub.Domain.Entities;

public class Photo
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Alt { get; set; } // Alt text for the photo
    public Rating Rating { get; set; }

    // Foreign Key for Spot
    public int SpotId { get; set; }
    // Navigation Property back to the spot
    public Spot Spot { get; set; }
}
