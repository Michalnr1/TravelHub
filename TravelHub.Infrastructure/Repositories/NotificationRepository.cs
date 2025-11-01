using Microsoft.EntityFrameworkCore;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;

namespace TravelHub.Infrastructure.Repositories;

public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
{
    public NotificationRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Notification>> GetByUserIdAsync(string userId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .Include(n => n.User)
            .OrderByDescending(n => n.ScheduledFor)
            .ToListAsync();
    }

    public async Task<IEnumerable<Notification>> GetPendingNotificationsAsync(DateTimeOffset currentTime)
    {
        return await _context.Notifications
            .Include(n => n.User)
            .Where(n => !n.IsSent && n.ScheduledFor <= currentTime)
            .ToListAsync();
    }
}
