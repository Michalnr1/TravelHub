using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Services;

public interface IActivityService : IGenericService<Activity>
{
    // Metody specyficzne:

    // Pobiera aktywności dla danego dnia, posortowane
    Task<IReadOnlyList<Activity>> GetOrderedDailyActivitiesAsync(int dayId);

    // Oblicza sumaryczny czas trwania wszystkich aktywności w danym dniu
    Task<decimal> CalculateDailyActivityDurationAsync(int dayId);

    // Zmienia kolejność aktywności w obrębie dnia
    Task ReorderActivitiesAsync(int dayId, List<(int activityId, int newOrder)> orderUpdates);
}
