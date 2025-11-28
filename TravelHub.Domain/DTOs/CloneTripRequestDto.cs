using TravelHub.Domain.Entities;

namespace TravelHub.Application.DTOs;

public class CloneTripRequestDto
{
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public CurrencyCode TargetCurrency { get; set; }
    public bool IsPrivate { get; set; } = true;

    // Elementy do sklonowania
    public bool CloneDays { get; set; } = true;
    public bool CloneGroups { get; set; } = true;
    public bool CloneAccommodations { get; set; } = true;
    public bool CloneActivities { get; set; } = true;
    public bool CloneSpots { get; set; } = true;
    public bool CloneTransport { get; set; } = true;
    public bool CloneExpenses { get; set; } = true;
}
