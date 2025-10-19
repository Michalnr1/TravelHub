using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TravelHub.Domain.Entities;

namespace TravelHub.Web.ViewModels.Transports;

public class TransportViewModel
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public TransportationType Type { get; set; }
    public decimal Duration { get; set; }
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
    public required string TripName { get; set; }
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
    [Range(0.1, 1000, ErrorMessage = "Duration must be between 0.1 and 1000 hours")]
    public decimal Duration { get; set; }

    [Required(ErrorMessage = "Trip is required")]
    [Display(Name = "Trip")]
    public int TripId { get; set; }

    [Required(ErrorMessage = "From spot is required")]
    [Display(Name = "From Spot")]
    public int FromSpotId { get; set; }

    [Required(ErrorMessage = "To spot is required")]
    [Display(Name = "To Spot")]
    public int ToSpotId { get; set; }

    // Select lists
    public List<TripSelectItem> Trips { get; set; } = new List<TripSelectItem>();
    public List<SpotSelectItem> Spots { get; set; } = new List<SpotSelectItem>();
    public List<TransportationTypeSelectItem> TransportationTypes { get; set; } = new List<TransportationTypeSelectItem>();
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