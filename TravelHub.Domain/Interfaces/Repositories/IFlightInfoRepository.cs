using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Repositories;

public interface IFlightInfoRepository
{
    Task<FlightInfo?> GetByIdAsync(int id);
    Task<IEnumerable<FlightInfo>> GetByTripIdAsync(int tripId);
    Task<IEnumerable<FlightInfo>> GetByTripAndUserAsync(int tripId, string userId);
    Task<FlightInfo> AddAsync(FlightInfo flightInfo);
    Task UpdateAsync(FlightInfo flightInfo);
    Task DeleteAsync(FlightInfo flightInfo);
    Task<bool> ExistsAsync(int id);
}
