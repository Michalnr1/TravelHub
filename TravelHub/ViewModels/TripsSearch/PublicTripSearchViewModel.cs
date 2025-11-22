using System.ComponentModel.DataAnnotations;
using TravelHub.Domain.Entities;

namespace TravelHub.Web.ViewModels.TripsSearch;

public class PublicTripSearchViewModel
{
    public string? SearchTerm { get; set; }
    public string? SelectedCountryCode { get; set; }

    [Display(Name = "Minimum days")]
    [Range(1, 365, ErrorMessage = "Days must be between 1 and 365")]
    public int? MinDays { get; set; }

    [Display(Name = "Maximum days")]
    [Range(1, 365, ErrorMessage = "Days must be between 1 and 365")]
    public int? MaxDays { get; set; }

    public List<Country> AvailableCountries { get; set; } = new();
    public List<PublicTripViewModel> Trips { get; set; } = new();

    public bool HasFilters => !string.IsNullOrEmpty(SearchTerm) ||
                            !string.IsNullOrEmpty(SelectedCountryCode) ||
                            MinDays.HasValue ||
                            MaxDays.HasValue;

    public bool HasResults => Trips.Any();
}

public class PublicTripViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int Duration => (EndDate - StartDate).Days + 1;
    public string? OwnerName { get; set; }
    public List<string> Countries { get; set; } = new();
    public int SpotsCount { get; set; }
    public int ParticipantsCount { get; set; }
    public bool IsOwnerPublic => !string.IsNullOrEmpty(OwnerName);

    public string DateRange => $"{StartDate:MMM dd, yyyy} - {EndDate:MMM dd, yyyy}";
    public string DurationDisplay => $"{Duration} day{(Duration > 1 ? "s" : "")}";
    public string OwnerDisplay => IsOwnerPublic ? OwnerName! : "Private User";
}
