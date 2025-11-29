namespace TravelHub.Domain.DTOs;

public class UpdateScheduledPostDto
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int? DayId { get; set; }
    public DateTime ScheduledFor { get; set; }
}