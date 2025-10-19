namespace TravelHub.Domain.Entities;

public class Transport
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public TransportationType Type { get; set; }
    public decimal Duration { get; set; }

    // Foreign Key for Trip
    public required int TripId { get; set; }
    // Navigation Property back to the trip
    public Trip? Trip { get; set; }

    // Foreign Key for the 'from' Spot
    public required int FromSpotId { get; set; }
    // Navigation Property for the departure spot
    public Spot? FromSpot { get; set; }

    // Foreign Key for the 'to' Spot
    public required int ToSpotId { get; set; }
    // Navigation Property for the arrival spot
    public Spot? ToSpot { get; set; }
}
