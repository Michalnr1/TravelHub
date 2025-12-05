using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelHub.Domain.Entities;

public class FlightInfo
{
    public int Id { get; set; }
    public int TripId { get; set; }
    public Trip? Trip { get; set; }

    // Podstawowe informacje
    public string? OriginAirportCode { get; set; }
    public string? DestinationAirportCode { get; set; }
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public TimeSpan Duration { get; set; }
    public decimal? Price { get; set; }
    public string? Currency { get; set; }

    // Opcjonalne szczegóły
    public string? Airline { get; set; }
    public List<string> FlightNumbers { get; set; } = new List<string>();
    public string? BookingReference { get; set; }
    public string? Notes { get; set; }

    // Status
    public bool IsConfirmed { get; set; }
    public DateTime AddedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }

    // Relacja do użytkownika
    public string PersonId { get; set; } = null!;

    [JsonIgnore]
    public Person AddedBy { get; set; } = null!;

    // JSON z segmentami
    public List<FlightSegment> Segments { get; set; } = new();

    // Właściwość wyliczana - lista przesiadek
    [NotMapped]
    public List<ConnectionInfo> Connections
    {
        get
        {
            var connections = new List<ConnectionInfo>();

            for (int i = 0; i < Segments.Count - 1; i++)
            {
                var currentSegment = Segments[i];
                var nextSegment = Segments[i + 1];

                if (currentSegment.ArrivalTime.HasValue && nextSegment.DepartureTime.HasValue)
                {
                    var connectionTime = nextSegment.DepartureTime.Value - currentSegment.ArrivalTime.Value;

                    connections.Add(new ConnectionInfo
                    {
                        AirportCode = currentSegment.DestinationAirportCode ?? "N/A",
                        ArrivalTime = currentSegment.ArrivalTime.Value,
                        DepartureTime = nextSegment.DepartureTime.Value,
                        Duration = connectionTime
                    });
                }
            }

            return connections;
        }
    }

    // Właściwość wyliczana - czy lot ma przesiadki
    [NotMapped]
    public bool HasConnections => Segments.Count > 1;

    // Właściwość wyliczana - całkowity czas przesiadek
    [NotMapped]
    public TimeSpan TotalConnectionTime
    {
        get
        {
            if (Segments.Count <= 1) return TimeSpan.Zero;

            TimeSpan total = TimeSpan.Zero;
            for (int i = 0; i < Segments.Count - 1; i++)
            {
                var currentSegment = Segments[i];
                var nextSegment = Segments[i + 1];

                if (currentSegment.ArrivalTime.HasValue && nextSegment.DepartureTime.HasValue)
                {
                    total += nextSegment.DepartureTime.Value - currentSegment.ArrivalTime.Value;
                }
            }

            return total;
        }
    }

    // Właściwość wyliczana - czysty czas lotu (bez przesiadek)
    [NotMapped]
    public TimeSpan PureFlightTime
    {
        get
        {
            if (HasConnections)
            {
                return Duration - TotalConnectionTime;
            }
            return Duration;
        }
    }
}

public record FlightSegment
{
    public string? OriginAirportCode { get; set; }
    public string? DestinationAirportCode { get; set; }
    public DateTime? DepartureTime { get; set; }
    public DateTime? ArrivalTime { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? CarrierCode { get; set; }
    public string? FlightNumber { get; set; }

    public string? FullFlightNumber =>
        !string.IsNullOrEmpty(CarrierCode) && !string.IsNullOrEmpty(FlightNumber)
            ? $"{CarrierCode}{FlightNumber}"
            : FlightNumber;
}

public class ConnectionInfo
{
    public string? AirportCode { get; set; }
    public DateTime ArrivalTime { get; set; }
    public DateTime DepartureTime { get; set; }
    public TimeSpan Duration { get; set; }

    public string FormattedDuration
    {
        get
        {
            if (Duration.TotalHours >= 1)
                return $"{(int)Duration.TotalHours}h {Duration.Minutes}m";
            return $"{Duration.Minutes}m";
        }
    }
}