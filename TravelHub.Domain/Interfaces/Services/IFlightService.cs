using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravelHub.Domain.DTOs;

namespace TravelHub.Domain.Interfaces.Services;

public interface IFlightService
{
    Task<List<FlightDto>> GetFlights(string fromAirportCode, string toAirportCode, DateTime departureDate, 
                                     int? adults, int? children, int? seated_infants, int? held_infants,
                                     string? currencyCode, int? maxPrice, int? maxStops);
    Task<AirportDto?> GetAirportByCoords(double latitude, double longitude);
    Task<List<AirportDto>> GetAirportsByName(string query);
}
