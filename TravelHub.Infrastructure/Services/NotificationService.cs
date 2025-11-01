using Microsoft.Extensions.Logging;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;

namespace TravelHub.Infrastructure.Services;

public class NotificationService : GenericService<Notification>, INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository notificationRepository,
        IEmailSender emailSender,
        ILogger<NotificationService> logger) : base(notificationRepository)
    {
        _notificationRepository = notificationRepository;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task ScheduleNotificationAsync(string userId, string title, string content, DateTimeOffset scheduledFor)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Content = content,
            ScheduledFor = scheduledFor,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _notificationRepository.AddAsync(notification);

        _logger.LogInformation("Scheduled notification {NotificationId} for user {UserId} at {ScheduledFor}",
            notification.Id, userId, scheduledFor);
    }

    public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId)
    {
        return await _notificationRepository.GetByUserIdAsync(userId);
    }

    public async Task ProcessPendingNotificationsAsync()
    {
        var currentTime = DateTimeOffset.UtcNow;
        var pendingNotifications = await _notificationRepository.GetPendingNotificationsAsync(currentTime);

        foreach (var notification in pendingNotifications)
        {
            try
            {
                await _emailSender.SendEmailAsync(
                    notification.User!.Email!,
                    notification.Title,
                    notification.Content);

                notification.IsSent = true;
                notification.SentAt = DateTime.UtcNow;

                await _notificationRepository.UpdateAsync(notification);

                _logger.LogInformation("Successfully sent notification {NotificationId} to {Email}",
                    notification.Id, notification.User!.Email!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification {NotificationId} to {Email}",
                    notification.Id, notification.User!.Email!);
            }
        }
    }

    public async Task<bool> CancelNotificationAsync(int notificationId, string userId)
    {
        var notification = await _notificationRepository.GetByIdAsync(notificationId);

        if (notification == null || notification.UserId != userId || notification.IsSent)
            return false;

        await _notificationRepository.DeleteAsync(notification);
        return true;
    }

    // Override metody DeleteAsync z GenericService jeśli potrzebujemy specyficznej logiki
    public new async Task DeleteAsync(object id)
    {
        var notification = await _notificationRepository.GetByIdAsync(id);
        if (notification != null)
        {
            // Możemy dodać dodatkową logikę przed usunięciem
            if (notification.IsSent)
            {
                throw new InvalidOperationException("Cannot delete a notification that has already been sent.");
            }

            await _notificationRepository.DeleteAsync(notification);
        }
    }
}