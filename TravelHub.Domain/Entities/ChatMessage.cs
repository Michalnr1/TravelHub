namespace TravelHub.Domain.Entities;

public class ChatMessage
{
    public int Id { get; set; }
    public required string Message { get; set; }

    public required string PersonId { get; set; }
    // Navigation Property for the person who owns the message (1:N)
    public Person? Person { get; set; }

    // Foreign Key for Trip
    public int TripId { get; set; }
    // Navigation Property to the trip
    public Trip? Trip { get; set; }
}
