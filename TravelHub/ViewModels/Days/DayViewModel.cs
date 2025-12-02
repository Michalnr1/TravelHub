using TravelHub.Domain.Entities;
using TravelHub.Web.ViewModels.Activities;

namespace TravelHub.Web.ViewModels.Days;

public class DayDetailsViewModel
{
    public int Id { get; set; }
    public int? Number { get; set; }
    public string? Name { get; set; }
    public DateTime Date { get; set; }
    public int TripId { get; set; }
    public Trip? Trip { get; set; }
    public int? AccommodationId { get; set; }
    public Accommodation? Accommodation { get; set; }

    public List<ActivityDetailsViewModel> Activities { get; set; } = new List<ActivityDetailsViewModel>();
    public List<ActivityDetailsViewModel> AvailableActivities { get; set; } = new List<ActivityDetailsViewModel>();
}

public class DayExportPdfViewModel
{
    public int Id { get; set; }
    public int? Number { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string TripName { get; set; } = string.Empty;
    public DateTime? TripStartDate { get; set; }
    public DateTime? TripEndDate { get; set; }
    public string StaticMapUrl { get; set; } = string.Empty;

    public List<ActivityExportViewModel> Activities { get; set; } = new();
    public List<SpotExportViewModel> Spots { get; set; } = new();
    public AccommodationExportViewModel? Accommodation { get; set; }
    public AccommodationExportViewModel? PreviousAccommodation { get; set; }

    // Dane mapy
    public double MapCenterLat { get; set; }
    public double MapCenterLng { get; set; }
    public double MapZoom { get; set; } = 12;

    // Dane trasy
    public RouteDataViewModel? RouteData { get; set; }

    // Checklist (jeśli Activity ma Checklist)
    public Checklist? Checklist { get; set; }

    // Flagi kontrolne dla widoku
    public bool HasRoute => RouteData != null && RouteData.Waypoints.Count > 1;
    public bool HasSpots => Spots.Any();
    public bool HasActivities => Activities.Any();
    public bool HasChecklist => Checklist?.Items != null && Checklist.Items.Any();

    public string GetDayTitle()
    {
        if (Number.HasValue)
            return $"Dzień {Number}: {Name}";
        return $"Grupa: {Name}";
    }

    public string GetDateRangeString()
    {
        if (TripStartDate.HasValue && TripEndDate.HasValue)
        {
            return $"{TripStartDate.Value:dd.MM.yyyy} - {TripEndDate.Value:dd.MM.yyyy}";
        }
        return Date.ToString("dd.MM.yyyy");
    }
}

public class ActivityExportViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Duration { get; set; }
    public string DurationString { get; set; } = string.Empty;
    public int Order { get; set; }
    public decimal? StartTime { get; set; }
    public string? StartTimeString { get; set; }
    public string? CategoryName { get; set; }
    public string Type { get; set; } = string.Empty;
    public Checklist? Checklist { get; set; }
}

public class SpotExportViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Duration { get; set; }
    public string DurationString { get; set; } = string.Empty;
    public int Order { get; set; }
    public decimal? StartTime { get; set; }
    public string? StartTimeString { get; set; }
    public string? CategoryName { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int PhotoCount { get; set; }
    public Checklist? Checklist { get; set; }
    // public Rating? Rating { get; set; }
}

public class AccommodationExportViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public decimal CheckInTime { get; set; }
    public decimal CheckOutTime { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public Checklist? Checklist { get; set; }

    public string GetCheckInTimeString()
    {
        return ConvertDecimalToTimeString(CheckInTime);
    }

    public string GetCheckOutTimeString()
    {
        return ConvertDecimalToTimeString(CheckOutTime);
    }

    private string ConvertDecimalToTimeString(decimal time)
    {
        int hours = (int)time;
        int minutes = (int)((time - hours) * 60);
        return $"{hours:D2}:{minutes:D2}";
    }
}

public class RouteDataViewModel
{
    public List<RoutePoint> Waypoints { get; set; } = new();
    public List<RouteSegment> Segments { get; set; } = new();
    public double TotalDistance { get; set; }
    public double TotalDuration { get; set; }
}

public class RoutePoint
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
}

public class RouteSegment
{
    public int From { get; set; }
    public int To { get; set; }
    public double Distance { get; set; }
    public double Duration { get; set; }
    public string Polyline { get; set; } = string.Empty;
}