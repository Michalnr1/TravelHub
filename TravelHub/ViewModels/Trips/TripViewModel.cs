using System.ComponentModel.DataAnnotations;
using TravelHub.Domain.Entities;

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

public class TripDetailViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Status Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<DayViewModel> Days { get; set; } = new();
    public int TransportsCount { get; set; }
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

    [Required]
    [Display(Name = "Day Number")]
    [Range(1, 365)]
    public int Number { get; set; } = 1;

    [Required]
    [Display(Name = "Date")]
    public DateTime Date { get; set; } = DateTime.Today;

    public DateTime MinDate { get; set; }
    public DateTime MaxDate { get; set; }
}

public class DayViewModel
{
    public int Id { get; set; }
    public int Number { get; set; }
    public DateTime Date { get; set; }
    public int ActivitiesCount { get; set; }
}