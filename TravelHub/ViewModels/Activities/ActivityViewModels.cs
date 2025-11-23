using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TravelHub.Domain.Entities;
using TravelHub.Web.ViewModels.Expenses;
using TravelHub.Web.ViewModels.Transports;

namespace TravelHub.Web.ViewModels.Activities;

public class ActivityViewModel
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Duration { get; set; }
    public string DurationString { get; set; } = "00:00";
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
    public string DurationString { get; set; } = "00:00";
    public int Order { get; set; }

    [Display(Name = "Category name")]
    public string? CategoryName { get; set; }

    [Display(Name = "Start time")]
    public decimal? StartTime { get; set; }

    [Display(Name = "Trip name")]
    public required string TripName { get; set; }
    public int TripId { get; set; }

    [Display(Name = "Day name")]
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
    [Display(Name = "Duration (hours:minutes)")]
    [RegularExpression(@"^([0-9]{1,2}):([0-5][0-9])$", ErrorMessage = "Please enter duration in format HH:MM")]
    public string DurationString { get; set; } = "00:00";

    public decimal Duration { get; set; }

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

    //[Required(ErrorMessage = "Cost is required")]
    //[Range(0, double.MaxValue, ErrorMessage = "Cost must be greater than or equal to 0")]
    //public decimal Cost { get; set; }

    // Sekcja Expense
    [Display(Name = "Estimated Cost")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Cost must be greater than 0")]
    public decimal? ExpenseValue { get; set; }

    [Display(Name = "Currency")]
    public CurrencyCode? ExpenseCurrencyCode { get; set; } = CurrencyCode.PLN;

    [Display(Name = "Exchange Rate (to Base)")]
    [Range(0.000001, (double)decimal.MaxValue, ErrorMessage = "Exchange Rate must be greater than 0")]
    public decimal? ExpenseExchangeRateValue { get; set; } = 1.0M;

    // Select lists dla Expense
    public List<CurrencySelectGroupItem> CurrenciesGroups { get; set; } = new List<CurrencySelectGroupItem>();
    public CurrencyCode TripCurrency { get; set; }

    public Rating? Rating { get; set; }
}

public class PhotoViewModel
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Alt { get; set; }
    public required string FilePath { get; set; }
}

public class FileViewModel
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int spotId { get; set; }
}

public class SpotDetailsViewModel : ActivityDetailsViewModel
{
    public double Longitude { get; set; }
    public double Latitude { get; set; }
    // public decimal Cost { get; set; }
    public Rating? Rating { get; set; }
    public IEnumerable<PhotoViewModel>? Photos { get; set; }
    public IEnumerable<FileViewModel>? Files { get; set; }
    public int PhotoCount { get; set; }
    public List<TransportBasicViewModel>? TransportsFrom { get; set; }
    public List<TransportBasicViewModel>? TransportsTo { get; set; }
}

public class AccommodationBasicViewModel : SpotDetailsViewModel
{
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public decimal CheckInTime { get; set; }
    public decimal CheckOutTime { get; set; }
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