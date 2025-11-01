using System.ComponentModel.DataAnnotations;

namespace TravelHub.Web.ViewModels.Notifications;

public class CreateNotificationViewModel
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, ErrorMessage = "Title cannot be longer than 200 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Content is required")]
    public string Content { get; set; } = string.Empty;

    [Required(ErrorMessage = "Date and time are required")]
    [Display(Name = "Scheduled for")]
    public DateTime ScheduledFor { get; set; } = DateTime.Now.AddHours(1);

    public string ScheduledForDateTimeOffset { get; set; } = string.Empty;
}