using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Services;

public interface ITripService
{
    Task<Trip?> GetTripByIdAsync(int id);
    Task<Trip?> GetTripWithDetailsAsync(int id);
    Task<IEnumerable<Trip>> GetUserTripsAsync(string userId);
    Task<Trip> CreateTripAsync(Trip trip);
    Task UpdateTripAsync(Trip trip);
    Task DeleteTripAsync(int id);
    Task<Day> AddDayToTripAsync(int tripId, Day day);
    Task<IEnumerable<Day>> GetTripDaysAsync(int tripId);
    Task<bool> UserOwnsTripAsync(int tripId, string userId);
    Task<bool> ExistsAsync(int id);
}
