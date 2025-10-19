using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TravelHub.Web.ViewModels.Activities;

public class ActivityViewModel
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Duration { get; set; }
    public int Order { get; set; }
    public string? CategoryName { get; set; }
    public required string TripName { get; set; }
    public string? DayName { get; set; }
}

public class ActivityDetailsViewModel
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Duration { get; set; }
    public int Order { get; set; }
    public string? CategoryName { get; set; }
    public required string TripName { get; set; }
    public string? DayName { get; set; }
    public string? Type { get; set; } // "Activity" or "Spot"
}

public class ActivityCreateEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot be longer than 500 characters")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Duration is required")]
    [Range(0, 24, ErrorMessage = "Duration must be between 0 and 24 hours")]
    public decimal Duration { get; set; }

    [Required(ErrorMessage = "Order is required")]
    [Range(1, 100, ErrorMessage = "Order must be between 1 and 100")]
    public int Order { get; set; }

    [Display(Name = "Category")]
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Trip is required")]
    [Display(Name = "Trip")]
    public int TripId { get; set; }

    [Display(Name = "Day")]
    public int? DayId { get; set; }

    // Select lists
    public List<CategorySelectItem> Categories { get; set; } = new List<CategorySelectItem>();
    public List<TripSelectItem> Trips { get; set; } = new List<TripSelectItem>();
    public List<DaySelectItem> Days { get; set; } = new List<DaySelectItem>();
}

public class SpotCreateEditViewModel : ActivityCreateEditViewModel
{
    [Required(ErrorMessage = "Longitude is required")]
    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
    public double Longitude { get; set; }

    [Required(ErrorMessage = "Latitude is required")]
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
    public double Latitude { get; set; }

    [Required(ErrorMessage = "Cost is required")]
    [Range(0, double.MaxValue, ErrorMessage = "Cost must be greater than or equal to 0")]
    public decimal Cost { get; set; }
}

public class SpotDetailsViewModel : ActivityDetailsViewModel
{
    public double Longitude { get; set; }
    public double Latitude { get; set; }
    public decimal Cost { get; set; }
    public int PhotoCount { get; set; }
    public int TransportsFromCount { get; set; }
    public int TransportsToCount { get; set; }
}

// Select list items
public class CategorySelectItem
{
    public int Id { get; set; }
    public required string Name { get; set; }
}

public class TripSelectItem
{
    public int Id { get; set; }
    public required string Name { get; set; }
}

public class DaySelectItem
{
    public int Id { get; set; }
    public int? Number {  get; set; }
    public string Name { get; set; } = string.Empty;
    public int TripId { get; set; }
}