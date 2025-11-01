using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Repositories;

public interface INotificationRepository : IGenericRepository<Notification>
{
    // Dodajemy tylko specyficzne metody, których nie ma w generic
    Task<Notification?> GetByIdWithUserAsync(int id);
    Task<IEnumerable<Notification>> GetByUserIdAsync(string userId);
    Task<IEnumerable<Notification>> GetPendingNotificationsAsync(DateTimeOffset currentTime);
}