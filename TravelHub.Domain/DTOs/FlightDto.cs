using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravelHub.Domain.Entities;

namespace TravelHub.Domain.DTOs;

public record FlightDto
{
    //public string? originCityName;
    public string? OriginAirportCode { get; set; }
    //public string? destinationCityName;
    public string? DestinationAirportCode { get; set; }
    public DateTime? DepartureTime { get; set; }
    public DateTime? ArrivalTime { get; set; }
    public TimeSpan? Duration { get; set; }
    public List<FlightSegmentDto>? Segments { get; set; } = new List<FlightSegmentDto>();
    public decimal? TotalPrice { get; set; }
    public string? Currency { get; set; }
}

public record FlightSegmentDto
{
    //public string? originCityName;
    public string? OriginAirportCode { get; set; }
    //public string? destinationCityName;
    public string? DestinationAirportCode { get; set; }
    public DateTime? DepartureTime { get; set; }
    public DateTime? ArrivalTime { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? CarrierCode { get; set; }
    public string? FlightNumber { get; set; }
}

public record AirportDto
{
    public string? AirportCode { get; set; }
    public string? AirportName { get; set; }
    public string? CityName { get; set; }
    public string? CountryName { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int? Distance { get; set; }
    public string? DistanceUnit { get; set; }

}