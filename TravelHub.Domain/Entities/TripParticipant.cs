namespace TravelHub.Domain.Entities;

public class TripParticipant
{
    public int Id { get; set; }

    // Foreign Key for Trip
    public int TripId { get; set; }
    public Trip Trip { get; set; } = null!;

    // Foreign Key for Person (participant)
    public string PersonId { get; set; } = null!;
    public Person Person { get; set; } = null!;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public TripParticipantStatus Status { get; set; } = TripParticipantStatus.Pending;
}

public enum TripParticipantStatus
{
    Pending,
    Accepted,
    Declined
}