using Hangfire;
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
            CreatedAt = DateTimeOffset.UtcNow,
            HangfireJobId = null // Będzie ustawione po utworzeniu joba
        };

        await _notificationRepository.AddAsync(notification);

        // Utwórz job w Hangfire
        var jobId = BackgroundJob.Schedule<INotificationService>(
            service => service.SendNotificationAsync(notification.Id),
            scheduledFor);

        // Zapisz job ID w notyfikacji
        notification.HangfireJobId = jobId;
        await _notificationRepository.UpdateAsync(notification);

        _logger.LogInformation("Scheduled notification {NotificationId} with Hangfire job {JobId} for {ScheduledFor}",
            notification.Id, jobId, scheduledFor);
    }

    public async Task SendNotificationAsync(int notificationId)
    {
        var notification = await _notificationRepository.GetByIdWithUserAsync(notificationId);

        if (notification == null)
        {
            _logger.LogWarning("Notification {NotificationId} not found", notificationId);
            return;
        }

        if (notification.IsSent)
        {
            _logger.LogWarning("Notification {NotificationId} already sent", notificationId);
            return;
        }

        try
        {
            // Pobierz użytkownika
            var user = notification.User;
            if (user == null)
            {
                _logger.LogError("User not found for notification {NotificationId}", notificationId);
                return;
            }

            await _emailSender.SendEmailAsync(
                user.Email!,
                notification.Title,
                notification.Content);

            notification.IsSent = true;
            notification.SentAt = DateTimeOffset.UtcNow;
            await _notificationRepository.UpdateAsync(notification);

            _logger.LogInformation("Successfully sent notification {NotificationId} to {Email}",
                notification.Id, user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification {NotificationId} to {Email}",
                notification.Id, notification.User?.Email);

            // Hangfire automatycznie ponowi przy failu (domyślnie do 10 razy)
            throw; // Rzuć wyjątek żeby Hangfire wiedział że job się nie udał
        }
    }

    public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId)
    {
        return await _notificationRepository.GetByUserIdAsync(userId);
    }

    public async Task<bool> CancelNotificationAsync(int notificationId, string userId)
    {
        var notification = await _notificationRepository.GetByIdAsync(notificationId);

        if (notification == null || notification.UserId != userId || notification.IsSent)
            return false;

        // Anuluj job w Hangfire jeśli istnieje
        if (!string.IsNullOrEmpty(notification.HangfireJobId))
        {
            BackgroundJob.Delete(notification.HangfireJobId);
            _logger.LogInformation("Cancelled Hangfire job {JobId} for notification {NotificationId}",
                notification.HangfireJobId, notificationId);
        }

        await _notificationRepository.DeleteAsync(notification);
        return true;
    }
}