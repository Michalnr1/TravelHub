using System.Collections.Generic;
using System.Threading.Tasks;
using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Services;

public interface ITripService : IGenericService<Trip>
{
    Task<Trip?> GetTripWithDetailsAsync(int id);
    Task<IEnumerable<Trip>> GetUserTripsAsync(string userId);
    Task<Day> AddDayToTripAsync(int tripId, Day day);
    Task<IEnumerable<Day>> GetTripDaysAsync(int tripId);
    Task<bool> UserOwnsTripAsync(int tripId, string userId);
    Task<bool> ExistsAsync(int id);
    Task<(double medianLatitude, double medianLongitude)> GetMedianCoords(int id);
}