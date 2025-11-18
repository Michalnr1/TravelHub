using System.ComponentModel.DataAnnotations;
using TravelHub.Domain.Entities;
using TravelHub.Web.ViewModels.Accommodations;
using TravelHub.Web.ViewModels.Activities;
using TravelHub.Web.ViewModels.Expenses;
using TravelHub.Web.ViewModels.Transports;

namespace TravelHub.Web.ViewModels.Trips;

public class TripViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Status Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsPrivate { get; set; } = true;
    public int DaysCount { get; set; }
    public int GroupsCount { get; set; }
    public int ParticipantsCount { get; set; }
    public bool IsOwner { get; set; }
    public TripParticipantStatus? UserParticipantStatus { get; set; }
    public int? ParticipantId { get; set; }
}

public class TripWithUserViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Status Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsPrivate { get; set; } = true;
    public int DaysCount { get; set; }
    public required Person Person { get; set; }
    public int ParticipantsCount { get; set; }
}

public class TripDetailViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Status Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsPrivate { get; set; } = true;
    public CurrencyCode CurrencyCode { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;

    // Collections
    public List<DayViewModel> Days { get; set; } = new();
    public List<ActivityViewModel> Activities { get; set; } = new();
    public List<SpotDetailsViewModel> Spots { get; set; } = new();
    public List<TransportViewModel> Transports { get; set; } = new();
    public List<AccommodationViewModel> Accommodations { get; set; } = new();
    public List<ExpenseViewModel> Expenses { get; set; } = new();
    public List<TripParticipantViewModel> Participants { get; set; } = new();
    public List<FriendViewModel> AvailableFriends { get; set; } = new();

    // Counts for display
    public int ActivitiesCount => Activities.Count;
    public int SpotsCount => Spots.Count;
    public int TransportsCount => Transports.Count;
    public int AccommodationsCount => Accommodations.Count;
    public int ExpensesCount => Expenses.Count;
    public int ParticipantsCount => Participants.Count(p => p.Status == TripParticipantStatus.Accepted);
    public int PendingInvitationsCount => Participants.Count(p => p.Status == TripParticipantStatus.Pending);
    public int EstimatedExpensesCount => Expenses.Count(e => e.IsEstimated);
    public decimal TotalExpenses => Expenses.Where(e => !e.IsEstimated).Sum(e => e.ConvertedValue);
    public decimal EstimatedExpensesTotal => Expenses.Where(e => e.IsEstimated).Sum(e => e.ConvertedValue * e.Multiplier);
    public decimal CombinedTotal => TotalExpenses + EstimatedExpensesTotal;

    // Helper properties
    public int Duration => (EndDate - StartDate).Days + 1;
    public string DateRange => $"{StartDate:MMM dd, yyyy} - {EndDate:MMM dd, yyyy}";
    public int NormalDaysCount => Days.Count(d => !d.IsGroup);
    public int GroupsCount => Days.Count(d => d.IsGroup);

    // Uprawnienia
    public bool IsCurrentUserOwner { get; set; }
    public bool CanEdit => IsCurrentUserOwner;
    public bool CanManageParticipants => IsCurrentUserOwner;

    // Formatowane wartości
    public string FormattedTotalExpenses => $"{TotalExpenses:N2} {CurrencyCode}";
    public string FormattedEstimatedExpensesTotal => $"{EstimatedExpensesTotal:N2} {CurrencyCode}";
    public string FormattedCombinedTotal => $"{CombinedTotal:N2} {CurrencyCode}";
}

public class CreateTripViewModel
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Start date")]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Required]
    [Display(Name = "End date")]
    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(7);

    [Required]
    [Display(Name = "Privacy")]
    public bool IsPrivate { get; set; } = true;

    [Required]
    [Display(Name = "Currency code")]
    public CurrencyCode CurrencyCode { get; set; }
}

public class EditTripViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Start date")]
    public DateTime StartDate { get; set; }

    [Required]
    [Display(Name = "End date")]
    public DateTime EndDate { get; set; }

    [Required]
    public Status Status { get; set; }

    [Required]
    [Display(Name = "Privacy")]
    public bool IsPrivate { get; set; } = true;

    [Required]
    [Display(Name = "Currency code")]
    public CurrencyCode CurrencyCode { get; set; }
}

public class AddDayViewModel
{
    public int TripId { get; set; }
    public string TripName { get; set; } = string.Empty;

    [Display(Name = "Day Number")]
    [Range(1, 365, ErrorMessage = "Day number must be between 1 and 365.")]
    public int? Number { get; set; }

    [Display(Name = "Name")]
    [StringLength(100)]
    public string? Name { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Date")]
    public DateTime Date { get; set; } = DateTime.Today;

    public DateTime MinDate { get; set; }
    public DateTime MaxDate { get; set; }

    public bool IsGroup { get; set; }
}

public class EditDayViewModel
{
    public int Id { get; set; }
    public int TripId { get; set; }
    public string TripName { get; set; } = string.Empty;

    [Display(Name = "Day Number")]
    [Range(1, 365, ErrorMessage = "Day number must be between 1 and 365.")]
    public int? Number { get; set; }

    [Display(Name = "Name")]
    [StringLength(100)]
    public string? Name { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Date")]
    public DateTime Date { get; set; } = DateTime.Today;

    public DateTime MinDate { get; set; }
    public DateTime MaxDate { get; set; }

    public bool IsGroup { get; set; }
}

public class DayViewModel
{
    public int Id { get; set; }
    public int? Number { get; set; }
    public string? Name { get; set; }
    public DateTime Date { get; set; }
    public int ActivitiesCount { get; set; }
    public bool IsGroup => Number == null && !string.IsNullOrWhiteSpace(Name);
    public string DisplayName => IsGroup
        ? Name ?? "Unnamed Group"
        : (Number.HasValue ? $"Day {Number}" : "Unnamed Day");
}

public class DayDetailViewModel
{
    public int Id { get; set; }
    public int? Number { get; set; }
    public string? Name { get; set; }
    public DateTime Date { get; set; }
    public TripViewModel? Trip { get; set; }
    public List<ActivityDetailsViewModel> Activities { get; set; } = new();
    public List<SpotDetailsViewModel> Spots { get; set; } = new();
    public AccommodationBasicViewModel? PreviousAccommodation { get; set; }
    public AccommodationBasicViewModel? NextAccommodation { get; set; }
    public bool IsGroup => Number == null && !string.IsNullOrWhiteSpace(Name);
    public string DisplayName => IsGroup
        ? Name ?? "Unnamed Group"
        : (Number.HasValue ? $"Day {Number}" : "Unnamed Day");
}

public class BasicActivityViewModel
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public decimal Duration { get; set; }
    public string? CategoryName { get; set; }
}

public class CountryViewModel
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SpotsCount { get; set; }
}