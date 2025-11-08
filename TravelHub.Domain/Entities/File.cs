namespace TravelHub.Domain.Entities;

public class File
{
    public int Id { get; set; }
    public required string Name { get; set; }

    // Foreign Key for Spot
    public int? SpotId { get; set; }
    // Navigation Property back to the spot
    public Spot? Spot { get; set; }
}
