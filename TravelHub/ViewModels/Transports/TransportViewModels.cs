using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TravelHub.Domain.Entities;
using TravelHub.Web.ViewModels.Expenses;

namespace TravelHub.Web.ViewModels.Transports;

public class TransportBasicViewModel
{
    public int Id { get; set; }
    public decimal Duration { get; set; }
    public string? Name { get; set; }
    public required int FromSpotId { get; set; }
    public required int ToSpotId { get; set; }
    public string? FromSpotName { get; set; }
    public string? ToSpotName { get; set; }
}

public class TransportViewModel
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public TransportationType Type { get; set; }
    public decimal Duration { get; set; }
    public string DurationString { get; set; } = "00:00";
    // public decimal Cost { get; set; }
    public required string TripName { get; set; }
    public required string FromSpotName { get; set; }
    public required string ToSpotName { get; set; }
}

public class TransportDetailsViewModel
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public TransportationType Type { get; set; }
    public decimal Duration { get; set; }
    // public decimal Cost { get; set; }
    public string DurationString { get; set; } = "00:00";
    public required string TripName { get; set; }
    public int TripId { get; set; }
    public required string FromSpotName { get; set; }
    public required string ToSpotName { get; set; }
    public string? FromSpotCoordinates { get; set; }
    public string? ToSpotCoordinates { get; set; }
}

public class TransportCreateEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Transportation type is required")]
    [Display(Name = "Transportation Type")]
    public TransportationType Type { get; set; }

    [Required(ErrorMessage = "Duration is required")]
    [Display(Name = "Duration (hours:minutes)")]
    [RegularExpression(@"^([0-9]{1,2}):([0-5][0-9])$", ErrorMessage = "Please enter duration in format HH:MM")]
    public string DurationString { get; set; } = "00:00";

    public decimal Duration { get; set; }

    //[Required(ErrorMessage = "Cost is required")]
    //[Range(0, double.MaxValue, ErrorMessage = "Cost must be greater than or equal to 0")]
    //public decimal Cost { get; set; }

    [Required(ErrorMessage = "Trip is required")]
    [Display(Name = "Trip")]
    public int TripId { get; set; }

    [Required(ErrorMessage = "From spot is required")]
    [Display(Name = "From Spot")]
    public int FromSpotId { get; set; }

    [Required(ErrorMessage = "To spot is required")]
    [Display(Name = "To Spot")]
    public int ToSpotId { get; set; }

    // Sekcja Expense - opcjonalna
    [Display(Name = "Estimated Cost")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Cost must be greater than 0")]
    public decimal? ExpenseValue { get; set; }

    [Display(Name = "Currency")]
    public CurrencyCode? ExpenseCurrencyCode { get; set; }

    [Display(Name = "Exchange Rate (to Base)")]
    [Range(0.000001, (double)decimal.MaxValue, ErrorMessage = "Exchange Rate must be greater than 0")]
    public decimal? ExpenseExchangeRateValue { get; set; } = 1.0M;
    public CurrencyCode TripCurrency {  get; set; }

    // Select lists
    public List<TripSelectItem> Trips { get; set; } = new List<TripSelectItem>();
    public List<SpotSelectItem> Spots { get; set; } = new List<SpotSelectItem>();
    public List<TransportationTypeSelectItem> TransportationTypes { get; set; } = new List<TransportationTypeSelectItem>();
    public IEnumerable<SelectListItem> SpotSelectList { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> TripSelectList { get; set; } = new List<SelectListItem>();

    // Select lists dla Expense
    public List<CurrencySelectGroupItem> CurrenciesGroups { get; set; } = new List<CurrencySelectGroupItem>();
}

// Select list items
public class TripSelectItem
{
    public int Id { get; set; }
    public required string Name { get; set; }
}

public class SpotSelectItem
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int TripId { get; set; }
    public required string Coordinates { get; set; }
}

public class TransportationTypeSelectItem
{
    public TransportationType Value { get; set; }
    public required string Name { get; set; }
}