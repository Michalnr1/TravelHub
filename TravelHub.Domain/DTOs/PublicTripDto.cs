namespace TravelHub.Domain.DTOs;

public class PublicTripDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? OwnerName { get; set; }
    public List<string> Countries { get; set; } = new();
    public int SpotsCount { get; set; }
    public int ParticipantsCount { get; set; }

    // Calculated properties
    public int Duration => (EndDate - StartDate).Days + 1;
    public bool IsOwnerPublic => !string.IsNullOrEmpty(OwnerName);
}
