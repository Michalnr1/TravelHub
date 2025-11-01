using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Web.ViewModels.Notifications;

namespace TravelHub.Web.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var notifications = await _notificationService.GetUserNotificationsAsync(userId!);
        return View(notifications);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateNotificationViewModel model)
    {
        // Custom validation for future date
        if (model.ScheduledFor <= DateTime.Now)
        {
            ModelState.AddModelError("ScheduledFor", "Please select a future date and time.");
        }

        if (string.IsNullOrEmpty(model.ScheduledForDateTimeOffset))
        {
            ModelState.AddModelError("", "Invalid date time format.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var scheduledForOffset = DateTimeOffset.Parse(model.ScheduledForDateTimeOffset);

            await _notificationService.ScheduleNotificationAsync(
                userId!,
                model.Title,
                model.Content,
                scheduledForOffset);

            TempData["Success"] = "Notification has been scheduled successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception)
        {
            ModelState.AddModelError("", "An error occurred while scheduling the notification. Please try again.");
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var result = await _notificationService.CancelNotificationAsync(id, userId!);

        if (result)
        {
            TempData["Success"] = "Notification has been cancelled successfully!";
        }
        else
        {
            TempData["Error"] = "Unable to cancel notification. It may have already been sent or doesn't exist.";
        }

        return RedirectToAction(nameof(Index));
    }
}
