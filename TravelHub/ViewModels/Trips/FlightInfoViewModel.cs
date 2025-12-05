using System.ComponentModel.DataAnnotations;
using TravelHub.Domain.Entities;

using ConnectionInfo = TravelHub.Domain.Entities.ConnectionInfo;

namespace TravelHub.Web.ViewModels.Trips;

public class FlightInfoViewModel
{
    public int Id { get; set; }
    public int TripId { get; set; }
    public string TripName { get; set; } = string.Empty;

    public string OriginAirportCode { get; set; } = null!;
    public string DestinationAirportCode { get; set; } = null!;

    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public TimeSpan Duration { get; set; }

    public decimal? Price { get; set; }
    public string? Currency { get; set; }

    public string? Airline { get; set; }
    public List<string> FlightNumbers { get; set; } = new List<string>();
    public string? BookingReference { get; set; }
    public string? Notes { get; set; }

    public bool IsConfirmed { get; set; }
    public DateTime AddedAt { get; set; }
    public string AddedByName { get; set; } = string.Empty;

    public List<FlightSegment> Segments { get; set; } = new List<FlightSegment>();

    public List<ConnectionInfo> Connections { get; set; } = new List<ConnectionInfo>();
    public TimeSpan TotalConnectionTime { get; set; }
    public TimeSpan PureFlightTime { get; set; }

    // Do wyświetlania
    public string FormattedDuration => $"{Duration.Hours}h {Duration.Minutes}m";
    public string PureFlightDuration => $"{(int)PureFlightTime.TotalHours}h {PureFlightTime.Minutes}m";
    public string ConnectionDuration => $"{TotalConnectionTime.Hours}h {TotalConnectionTime.Minutes}m";

    public string DepartureDate => DepartureTime.ToString("ddd, MMM dd");
    public string ArrivalDate => ArrivalTime.ToString("ddd, MMM dd");
    public string DepartureTimeShort => DepartureTime.ToString("HH:mm");
    public string ArrivalTimeShort => ArrivalTime.ToString("HH:mm");

    public string DisplayPrice => Price.HasValue ?
        $"{Price.Value:F2} {Currency}" : "Price not set";

    public string StopsInfo => Segments.Count <= 1 ? "Direct" :
        $"{Segments.Count - 1} stop{(Segments.Count - 1 > 1 ? "s" : "")}";

    public string AllFlightNumbers =>
        string.Join(" + ", Segments
            .Where(s => !string.IsNullOrEmpty(s.FullFlightNumber))
            .Select(s => s.FullFlightNumber));

    public bool HasMultipleSegments => Segments.Count > 1;

    public void CalculateConnectionInfo()
    {
        Connections.Clear();

        if (Segments.Count > 1)
        {
            for (int i = 0; i < Segments.Count - 1; i++)
            {
                var currentSegment = Segments[i];
                var nextSegment = Segments[i + 1];

                if (currentSegment.ArrivalTime.HasValue && nextSegment.DepartureTime.HasValue)
                {
                    var connectionTime = nextSegment.DepartureTime.Value - currentSegment.ArrivalTime.Value;

                    Connections.Add(new ConnectionInfo
                    {
                        AirportCode = currentSegment.DestinationAirportCode ?? "N/A",
                        ArrivalTime = currentSegment.ArrivalTime.Value,
                        DepartureTime = nextSegment.DepartureTime.Value,
                        Duration = connectionTime
                    });
                }
            }
        }
    }
}

public class AddFlightViewModel
{
    public int TripId { get; set; }

    [Required]
    [StringLength(10)]
    [Display(Name = "From (Airport Code)")]
    public string OriginAirportCode { get; set; } = null!;

    [Required]
    [StringLength(10)]
    [Display(Name = "To (Airport Code)")]
    public string DestinationAirportCode { get; set; } = null!;

    [Required]
    [Display(Name = "Departure Time")]
    public DateTime DepartureTime { get; set; }

    [Required]
    [Display(Name = "Arrival Time")]
    public DateTime ArrivalTime { get; set; }

    [Display(Name = "Price")]
    [Range(0, 1000000)]
    public decimal? Price { get; set; }

    [StringLength(3)]
    [Display(Name = "Currency")]
    public string? Currency { get; set; }

    [StringLength(50)]
    [Display(Name = "Airline")]
    public string? Airline { get; set; }

    [StringLength(500)]
    [Display(Name = "Flight Numbers (separate with comma)")]
    public string? FlightNumbers { get; set; }

    [StringLength(50)]
    [Display(Name = "Booking Reference")]
    public string? BookingReference { get; set; }

    [StringLength(500)]
    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    [Display(Name = "Mark as Confirmed")]
    public bool IsConfirmed { get; set; }

    // Nowe właściwości dla wielu segmentów
    public List<FlightSegmentViewModel> Segments { get; set; } = new List<FlightSegmentViewModel>
    {
        new FlightSegmentViewModel() // Domyślnie jeden segment
    };

    [Display(Name = "Number of Segments")]
    public int NumberOfSegments { get; set; } = 1;
}

public class FlightSegmentViewModel
{
    [Required]
    [StringLength(10)]
    [Display(Name = "From")]
    public string OriginAirportCode { get; set; } = null!;

    [Required]
    [StringLength(10)]
    [Display(Name = "To")]
    public string DestinationAirportCode { get; set; } = null!;

    [Required]
    [Display(Name = "Departure")]
    public DateTime DepartureTime { get; set; }

    [Required]
    [Display(Name = "Arrival")]
    public DateTime ArrivalTime { get; set; }

    [StringLength(2)]
    [Display(Name = "Airline Code")]
    public string? CarrierCode { get; set; }

    [StringLength(10)]
    [Display(Name = "Flight Number")]
    public string? FlightNumber { get; set; }
}

public class EditFlightViewModel : AddFlightViewModel
{
    public int Id { get; set; }
}

public class FlightListViewModel
{
    public int TripId { get; set; }
    public string TripName { get; set; } = string.Empty;
    public List<FlightInfoViewModel> Flights { get; set; } = new List<FlightInfoViewModel>();
}