using System.ComponentModel.DataAnnotations;
using TravelHub.Domain.Entities;
using TravelHub.Web.ViewModels.Activities;
using TravelHub.Web.ViewModels.Transports;

namespace TravelHub.Web.ViewModels.Trips;

public class TripViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Status Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int DaysCount { get; set; }
}

public class TripWithUserViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Status Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int DaysCount { get; set; }
    public required Person Person { get; set; }
}

public class TripDetailViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Status Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Collections
    public List<DayViewModel> Days { get; set; } = new();
    public List<ActivityViewModel> Activities { get; set; } = new();
    public List<SpotDetailsViewModel> Spots { get; set; } = new();
    public List<TransportViewModel> Transports { get; set; } = new();

    // Counts for display
    public int ActivitiesCount => Activities.Count;
    public int SpotsCount => Spots.Count;
    public int TransportsCount => Transports.Count;
}

public class CreateTripViewModel
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Start Date")]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Required]
    [Display(Name = "End Date")]
    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(7);
}

public class EditTripViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Start Date")]
    public DateTime StartDate { get; set; }

    [Required]
    [Display(Name = "End Date")]
    public DateTime EndDate { get; set; }

    [Required]
    public Status Status { get; set; }
}

public class AddDayViewModel
{
    public int TripId { get; set; }
    public string TripName { get; set; } = string.Empty;

    [Display(Name = "Day Number")]
    [Range(1, 365)]
    public int Number { get; set; }

    [Display(Name = "Name")]
    public string? Name { get; set; }

    [Required]
    [Display(Name = "Date")]
    public DateTime Date { get; set; } = DateTime.Today;

    public DateTime MinDate { get; set; }
    public DateTime MaxDate { get; set; }
}

public class DayViewModel
{
    public int Id { get; set; }
    public int? Number { get; set; }
    public string? Name { get; set; }
    public DateTime Date { get; set; }
    public int ActivitiesCount { get; set; }
}

public class BasicActivityViewModel
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public decimal Duration { get; set; }
    public string? CategoryName { get; set; }
}