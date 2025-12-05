using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Services;

public interface IFlightInfoService
{
    Task<FlightInfo?> GetByIdAsync(int id);
    Task<IEnumerable<FlightInfo>> GetByTripIdAsync(int tripId);
    Task<IEnumerable<FlightInfo>> GetByTripAndUserAsync(int tripId, string userId);
    Task<FlightInfo> AddAsync(FlightInfo flightInfo);
    Task UpdateAsync(FlightInfo flightInfo);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task ToggleConfirmationAsync(int id, bool isConfirmed);
    Task<bool> UserCanModifyFlightAsync(int flightId, string userId);
}