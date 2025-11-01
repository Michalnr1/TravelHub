using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Services;

public interface INotificationService : IGenericService<Notification>
{
    // Dodajemy tylko specyficzne metody, których nie ma w generic
    Task ScheduleNotificationAsync(string userId, string title, string content, DateTimeOffset scheduledFor);
    Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId);
    Task ProcessPendingNotificationsAsync();
    Task<bool> CancelNotificationAsync(int notificationId, string userId);
}