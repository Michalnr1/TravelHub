using System.ComponentModel.DataAnnotations;

namespace TravelHub.Domain.Entities;

public class Notification
{
    public int Id { get; set; }

    public required string UserId { get; set; }
    public Person? User { get; set; }

    [MaxLength(200)]
    public required string Title { get; set; }

    public required string Content { get; set; }

    public DateTimeOffset ScheduledFor { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public bool IsSent { get; set; } = false;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? HangfireJobId { get; set; }
}