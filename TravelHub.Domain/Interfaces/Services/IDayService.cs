using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Services;

public interface IDayService : IGenericService<Day>
{
    Task<Day?> GetDayWithDetailsAsync(int id);
    Task<Day?> GetDayByIdAsync(int id);
    Task<bool> UserOwnsDayAsync(int dayId, string userId);
    Task<bool> IsDayAGroupAsync(int dayId);
    Task<bool> ValidateDateRangeAsync(int tripId, DateTime date);
}
