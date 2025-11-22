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

    Task<IEnumerable<Activity>> GetAllWithDetailsAsync();

    Task<IEnumerable<Activity>> GetTripActivitiesWithDetailsAsync(int tripId);

    Task<bool> UserOwnsActivityAsync(int activityId, string userId);

    Task<Activity?> GetActivityWithTripAndParticipantsAsync(int activityId);
    Task AddChecklistItemAsync(int activityId, string item);
    Task ToggleChecklistItemAsync(int activityId, string itemTitle);
    Task AssignParticipantToItemAsync(int activityId, string itemTitle, string? participantId);
    Task RemoveChecklistItemAsync(int activityId, string itemTitle);
    Task RenameChecklistItemAsync(int activityId, string oldTitle, string newTitle);
}
