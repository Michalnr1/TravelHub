using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Services;

public interface IDayService : IGenericService<Day>
{
    Task<Day?> GetDayWithDetailsAsync(int id);
    Task<Day?> GetDayByIdAsync(int id);
    Task<bool> UserOwnsDayAsync(int dayId, string userId);
    Task<bool> IsDayAGroupAsync(int dayId);
    Task<bool> ValidateDateRangeAsync(int tripId, DateTime date);
    Task AddAccommodationToDay(int dayId, int accommodationId);
    Task<IEnumerable<Day>> GetDaysByTripIdAsync(int tripId);
    Task<(double medianLatitude, double medianLongitude)> GetMedianCoords(int id);
}
