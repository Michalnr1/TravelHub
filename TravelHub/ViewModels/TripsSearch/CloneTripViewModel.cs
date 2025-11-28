using System.ComponentModel.DataAnnotations;
using TravelHub.Domain.Entities;

namespace TravelHub.Web.ViewModels.TripsSearch;

public class CloneTripViewModel
{
    public int SourceTripId { get; set; }
    public string SourceTripName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Trip Name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Start Date")]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Required]
    [Display(Name = "End Date")]
    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(7);

    [Required]
    [Display(Name = "Currency")]
    public CurrencyCode TargetCurrency { get; set; } = CurrencyCode.PLN;

    [Display(Name = "Make trip private")]
    public bool IsPrivate { get; set; } = true;

    // Elementy do sklonowania
    [Display(Name = "Clone Days")]
    public bool CloneDays { get; set; } = true;

    [Display(Name = "Clone Groups")]
    public bool CloneGroups { get; set; } = true;

    [Display(Name = "Clone Accommodations")]
    public bool CloneAccommodations { get; set; } = true;

    [Display(Name = "Clone Activities")]
    public bool CloneActivities { get; set; } = true;

    [Display(Name = "Clone Spots")]
    public bool CloneSpots { get; set; } = true;

    [Display(Name = "Clone Transport")]
    public bool CloneTransport { get; set; } = true;

    [Display(Name = "Clone Estimated Expenses")]
    public bool CloneExpenses { get; set; } = true;
}