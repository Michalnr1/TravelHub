using System.Text;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Extensions;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Web.ViewModels.Activities;

namespace TravelHub.Web.Controllers;

[Authorize]
public class ActivitiesController : Controller
{
    private readonly IActivityService _activityService;
    private readonly IGenericService<Category> _categoryService;
    private readonly ITripService _tripService;
    private readonly IGenericService<Day> _dayService;
    private readonly UserManager<Person> _userManager;
    private readonly ILogger<ActivitiesController> _logger;

    public ActivitiesController(
        IActivityService activityService,
        IGenericService<Category> categoryService,
        ITripService tripService,
        IGenericService<Day> dayService,
        ILogger<ActivitiesController> logger,
         UserManager<Person> userManager)
    {
        _activityService = activityService;
        _categoryService = categoryService;
        _tripService = tripService;
        _dayService = dayService;
        _logger = logger;
        _userManager = userManager;
    }

    // GET: Activities
    public async Task<IActionResult> Index()
    {
        var activities = await _activityService.GetAllWithDetailsAsync();
        var viewModel = activities.Select(a => new ActivityViewModel
        {
            Id = a.Id,
            Name = a.Name,
            Description = a.Description!,
            Duration = a.Duration,
            DurationString = ConvertDecimalToTimeString(a.Duration),
            Order = a.Order,
            CategoryName = a.Category?.Name,
            TripName = a.Trip?.Name!,
            DayName = a.Day?.Name
        }).ToList();

        return View(viewModel);
    }

    // GET: Activities/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var activity = await _activityService.GetByIdAsync(id.Value);
        if (activity == null)
        {
            return NotFound();
        }

        if (!await _activityService.UserOwnsActivityAsync(id.Value, GetCurrentUserId()))
        {
            return Forbid();
        }

        var viewModel = new ActivityDetailsViewModel
        {
            Id = activity.Id,
            Name = activity.Name,
            Description = activity.Description!,
            Duration = activity.Duration,
            DurationString = ConvertDecimalToTimeString(activity.Duration),
            Order = activity.Order,
            CategoryName = activity.Category?.Name,
            TripName = activity.Trip?.Name!,
            TripId = activity.TripId,
            DayName = activity.Day?.Name,
            Type = activity is Spot ? "Spot" : "Activity"
        };

        return View(viewModel);
    }

    // GET: Activities/Create
    public async Task<IActionResult> Create()
    {
        var viewModel = await CreateActivityCreateEditViewModel();
        viewModel.Order = 0;
        viewModel.DurationString = "01:00";
        return View(viewModel);
    }

    // POST: Activities/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ActivityCreateEditViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            viewModel.Duration = ConvertTimeStringToDecimal(viewModel.DurationString);
            viewModel.Order = await CalculateNextOrder(viewModel.DayId);

            var activity = new Activity
            {
                Name = viewModel.Name,
                Description = viewModel.Description!,
                Duration = viewModel.Duration,
                Order = viewModel.Order,
                CategoryId = viewModel.CategoryId,
                TripId = viewModel.TripId,
                DayId = viewModel.DayId
            };

            await _activityService.AddAsync(activity);
            return RedirectToAction(nameof(Index));
        }

        await PopulateSelectLists(viewModel);
        return View(viewModel);
    }

    // GET: Activities/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        if (!await _activityService.UserOwnsActivityAsync(id.Value, GetCurrentUserId()))
        {
            return Forbid();
        }

        var activity = await _activityService.GetByIdAsync(id.Value);
        if (activity == null)
        {
            return NotFound();
        }

        var viewModel = await CreateActivityCreateEditViewModel(activity);
        viewModel.DurationString = ConvertDecimalToTimeString(activity.Duration);
        return View(viewModel);
    }

    // POST: Activities/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ActivityCreateEditViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return NotFound();
        }

        if (!await _activityService.UserOwnsActivityAsync(id, GetCurrentUserId()))
        {
            return Forbid();
        }

        if (ModelState.IsValid)
        {
            try
            {
                viewModel.Duration = ConvertTimeStringToDecimal(viewModel.DurationString);

                var existingActivity = await _activityService.GetByIdAsync(id);
                if (existingActivity == null)
                {
                    return NotFound();
                }

                var oldDayId = existingActivity.DayId;

                // Jeśli zmieniono dzień, przelicz Order
                if (existingActivity.DayId != viewModel.DayId)
                {
                    viewModel.Order = await CalculateNextOrder(viewModel.DayId);
                }

                existingActivity.Name = viewModel.Name;
                existingActivity.Description = viewModel.Description!;
                existingActivity.Duration = viewModel.Duration;
                existingActivity.Order = viewModel.Order;
                existingActivity.CategoryId = viewModel.CategoryId;
                existingActivity.TripId = viewModel.TripId;
                existingActivity.DayId = viewModel.DayId;

                await _activityService.UpdateAsync(existingActivity);
                await Task.Delay(50);

                // Jeśli zmieniono dzień, przelicz Order w starym i nowym dniu
                if (oldDayId != viewModel.DayId)
                {
                    await RecalculateOrdersForBothDays(oldDayId, viewModel.DayId);
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ActivityExists(viewModel.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction("Details", "Trips", new { id = viewModel.TripId });
        }

        await PopulateSelectLists(viewModel);
        return View(viewModel);
    }

    // GET: Activities/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var activity = await _activityService.GetByIdAsync(id.Value);
        if (activity == null)
        {
            return NotFound();
        }

        if (!await _activityService.UserOwnsActivityAsync(id.Value, GetCurrentUserId()))
        {
            return Forbid();
        }

        var viewModel = new ActivityDetailsViewModel
        {
            Id = activity.Id,
            Name = activity.Name,
            Description = activity.Description!,
            Duration = activity.Duration,
            DurationString = ConvertDecimalToTimeString(activity.Duration),
            Order = activity.Order,
            CategoryName = activity.Category?.Name,
            TripName = activity.Trip?.Name!,
            TripId = activity.TripId,
            DayName = activity.Day?.Name,
            Type = activity is Spot ? "Spot" : "Activity"
        };

        return View(viewModel);
    }

    // POST: Activities/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var activity = await _activityService.GetByIdAsync(id);
        if (!await _activityService.UserOwnsActivityAsync(id, GetCurrentUserId()))
        {
            return Forbid();
        }
        if (activity != null)
        {
            var dayId = activity.DayId;
            await _activityService.DeleteAsync(id);

            // Przelicz Order w dniu po usunięciu aktywności
            await RecalculateOrderForDay(dayId);

            return RedirectToAction("Details", "Trips", new { id = activity.TripId });
        }
        else
        {
            return NotFound();
        }
    }

    // GET: Activities/AddToTrip/5
    public async Task<IActionResult> AddToTrip(int tripId, int? dayId = null)
    {
        var trip = await _tripService.GetByIdAsync(tripId);
        if (trip == null)
        {
            return NotFound();
        }

        if (!UserOwnsTrip(trip))
        {
            return Forbid();
        }

        var viewModel = new ActivityCreateEditViewModel
        {
            TripId = tripId,
            Order = await CalculateNextOrder(dayId),
            DayId = dayId
        };

        await PopulateSelectListsForTrip(viewModel, tripId);

        SetAddToTripViewData(trip, dayId);

        return View("AddToTrip", viewModel);
    }

    // POST: Activities/AddToTrip/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToTrip(int tripId, ActivityCreateEditViewModel viewModel)
    {
        if (tripId != viewModel.TripId)
        {
            return NotFound();
        }

        var trip = await _tripService.GetByIdAsync(tripId);
        if (trip == null)
        {
            return NotFound();
        }

        if (!UserOwnsTrip(trip))
        {
            return Forbid();
        }

        if (ModelState.IsValid)
        {
            try
            {
                viewModel.Duration = ConvertTimeStringToDecimal(viewModel.DurationString);
                viewModel.Order = await CalculateNextOrder(viewModel.DayId);

                var activity = new Activity
                {
                    Name = viewModel.Name,
                    Description = viewModel.Description!,
                    Duration = viewModel.Duration,
                    Order = viewModel.Order,
                    CategoryId = viewModel.CategoryId,
                    TripId = viewModel.TripId,
                    DayId = viewModel.DayId
                };

                await _activityService.AddAsync(activity);

                TempData["SuccessMessage"] = "Activity added successfully!";
                return RedirectToAction("Details", "Trips", new { id = tripId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding activity to trip");
                ModelState.AddModelError("", "An error occurred while adding the activity.");
            }
        }

        await PopulateSelectListsForTrip(viewModel, tripId);
        SetAddToTripViewData(trip, viewModel.DayId);
        return View("AddToTrip", viewModel);
    }

    private void SetAddToTripViewData(Trip trip, int? dayId)
    {
        ViewData["TripName"] = trip.Name;
        ViewData["DayName"] = dayId.HasValue ?
            trip.Days?.FirstOrDefault(d => d.Id == dayId)?.Name : null;
        if (dayId != null)
        {
            ViewData["ReturnUrl"] = Url.Action("Details", "Days", new { id = dayId });
        }
        else
        {
            ViewData["ReturnUrl"] = Url.Action("Details", "Trips", new { id = trip.Id });
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddToDay(int activityId, int dayId)
    {
        var activity = await _activityService.GetByIdAsync(activityId);
        if (activity == null)
        {
            return NotFound();
        }

        // Blokada dla Accommodation
        if (activity is Accommodation)
        {
            TempData["ErrorMessage"] = "Accommodations are automatically assigned based on check-in/check-out dates and cannot be manually assigned to days.";
            return RedirectToAction("Details", "Days", new { id = dayId });
        }

        // Zapisz stary DayId dla przeliczenia order
        var oldDayId = activity.DayId;

        // Ustaw nowy DayId
        activity.DayId = dayId;

        // Oblicz nowy Order dla aktywności w nowym dniu
        activity.Order = await CalculateNextOrder(dayId);

        await _activityService.UpdateAsync(activity);

        // Przeczyszcz order w starym i nowym dniu
        await RecalculateOrdersForBothDays(oldDayId, dayId);

        string activityType = activity switch
        {
            Accommodation => "accommodation",
            Spot => "spot",
            _ => "activity"
        };

        TempData["SuccessMessage"] = $"{activityType.FirstCharToUpper()} added to day successfully!";
        return RedirectToAction("Details", "Days", new { id = dayId });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveFromDay(int activityId, int dayId)
    {
        var activity = await _activityService.GetByIdAsync(activityId);
        if (activity == null)
        {
            return NotFound();
        }

        // Zapisz stary DayId dla przeliczenia order
        var oldDayId = activity.DayId;

        // Ustaw DayId na null i zresetuj Order
        activity.DayId = null;
        activity.Order = 0;

        await _activityService.UpdateAsync(activity);

        // Przeczyszcz order tylko w starym dniu (bo nowy to null)
        await RecalculateOrderForDay(oldDayId);

        string activityType = activity switch
        {
            Accommodation => "accommodation",
            Spot => "spot",
            _ => "activity"
        };

        TempData["SuccessMessage"] = $"{activityType.FirstCharToUpper()} removed from day successfully!";
        return RedirectToAction("Details", "Days", new { id = dayId });
    }

    private async Task PopulateSelectListsForTrip(ActivityCreateEditViewModel viewModel, int tripId)
    {
        // Categories
        var categories = await _categoryService.GetAllAsync();
        viewModel.Categories = categories.Select(c => new CategorySelectItem
        {
            Id = c.Id,
            Name = c.Name
        }).ToList();

        // Days - only for this trip
        var days = await _dayService.GetAllAsync();
        viewModel.Days = days.Where(d => d.TripId == tripId)
            .Select(d => new DaySelectItem
            {
                Id = d.Id,
                Number = d.Number,
                Name = d.Name!,
                TripId = d.TripId
            }).ToList();
    }

    private async Task<bool> ActivityExists(int id)
    {
        var activity = await _activityService.GetByIdAsync(id);
        return activity != null;
    }

    private async Task<ActivityCreateEditViewModel> CreateActivityCreateEditViewModel(Activity? activity = null)
    {
        var viewModel = new ActivityCreateEditViewModel();

        if (activity != null)
        {
            viewModel.Id = activity.Id;
            viewModel.Name = activity.Name;
            viewModel.Description = activity.Description;
            viewModel.Duration = activity.Duration;
            viewModel.Order = activity.Order;
            viewModel.CategoryId = activity.CategoryId;
            viewModel.TripId = activity.TripId;
            viewModel.DayId = activity.DayId;
        }

        await PopulateSelectLists(viewModel);
        return viewModel;
    }

    private async Task PopulateSelectLists(ActivityCreateEditViewModel viewModel)
    {
        // Categories
        var categories = await _categoryService.GetAllAsync();
        viewModel.Categories = categories.Select(c => new CategorySelectItem
        {
            Id = c.Id,
            Name = c.Name
        }).ToList();

        // Trips
        var trips = await _tripService.GetAllAsync();
        viewModel.Trips = trips.Select(t => new TripSelectItem
        {
            Id = t.Id,
            Name = t.Name
        }).ToList();

        // Days - filter by selected trip if available
        var days = await _dayService.GetAllAsync();
        if (viewModel.TripId > 0)
        {
            days = days.Where(d => d.TripId == viewModel.TripId).ToList();
        }
        viewModel.Days = days.Select(d => new DaySelectItem
        {
            Id = d.Id,
            Number = d.Number,
            Name = d.Name!,
            TripId = d.TripId
        }).ToList();
    }

    /// <summary>
    /// Calculates the next Order value for an activity in a specific day
    /// </summary>
    /// <param name="dayId">The day ID (nullable)</param>
    /// <returns>0 if no day is selected, otherwise the highest Order + 1 for the selected day</returns>
    private async Task<int> CalculateNextOrder(int? dayId)
    {
        if (!dayId.HasValue || dayId == 0)
        {
            return 0;
        }

        // Pobierz wszystkie aktywności dla danego dnia
        var itemsInDay = await _activityService.GetAllAsync();
        itemsInDay = itemsInDay.Where(a => a.DayId == dayId && !(a is Accommodation)).ToList();

        if (!itemsInDay.Any())
        {
            return 1; // Pierwsza aktywność w tym dniu
        }

        // Znajdź najwyższe Order i dodaj 1
        var maxOrder = itemsInDay.Max(a => a.Order);
        return maxOrder + 1;
    }

    /// <summary>
    /// Recalculates Order for all activities in a day to remove gaps
    /// </summary>
    private async Task RecalculateOrderForDay(int? dayId)
    {
        if (!dayId.HasValue || dayId == 0)
            return;

        var activitiesInDay = await _activityService.GetAllAsync();
        activitiesInDay = activitiesInDay
            .Where(a => a.DayId == dayId && !(a is Accommodation))
            .OrderBy(a => a.Order)
            .ToList();

        if (!activitiesInDay.Any())
            return;

        // Reset orders sequentially starting from 1
        int newOrder = 1;
        foreach (var activity in activitiesInDay)
        {
            activity.Order = newOrder++;
            await _activityService.UpdateAsync(activity);
        }
    }

    /// <summary>
    /// Recalculates Order for both old and new days when moving activity between days
    /// </summary>
    private async Task RecalculateOrdersForBothDays(int? oldDayId, int? newDayId)
    {
        if (oldDayId.HasValue && oldDayId > 0)
        {
            await RecalculateOrderForDay(oldDayId);
        }

        if (newDayId.HasValue && newDayId > 0)
        {
            await RecalculateOrderForDay(newDayId);
        }
    }

    /// <summary>
    /// Konwertuje czas w formacie string (HH:MM) na decimal (godziny)
    /// </summary>
    private decimal ConvertTimeStringToDecimal(string timeString)
    {
        if (string.IsNullOrEmpty(timeString))
            return 0;

        var parts = timeString.Split(':');
        if (parts.Length != 2)
            return 0;

        if (int.TryParse(parts[0], out int hours) && int.TryParse(parts[1], out int minutes))
        {
            return hours + (minutes / 60.0m);
        }

        return 0;
    }

    /// <summary>
    /// Konwertuje decimal (godziny) na string w formacie HH:MM
    /// </summary>
    private string ConvertDecimalToTimeString(decimal duration)
    {
        int hours = (int)duration;
        int minutes = (int)((duration - hours) * 60);
        return $"{hours:D2}:{minutes:D2}";
    }

    private string GetCurrentUserId()
    {
        return _userManager.GetUserId(User) ?? throw new UnauthorizedAccessException("User is not authenticated");
    }

    private bool UserOwnsTrip(Trip trip)
    {
        return trip.PersonId == GetCurrentUserId();
    }

    // GET: Activities/Export/5
    public async Task<IActionResult> Export(int? id)
    {
        if (id == null) return NotFound();

        var activity = await _activityService.GetByIdAsync(id.Value);
        if (activity == null) return NotFound();

        string name = activity.Name ?? string.Empty;
        string description = activity.Description ?? string.Empty;
        string duration = FormatDuration(activity.Duration);

        var sb = new StringBuilder();
        sb.AppendLine("Activity name:");
        sb.AppendLine(name);
        sb.AppendLine();
        sb.AppendLine("Description:");
        sb.AppendLine(description);
        sb.AppendLine();
        sb.AppendLine("Duration:");
        sb.AppendLine(duration);

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"activity_{activity.Name}.txt";
        return File(bytes, "text/plain; charset=utf-8", fileName);
    }

    private static string FormatDuration(decimal hoursDecimal)
    {
        // Convert decimal hours to H:MM (e.g., 1.5 -> "1:30")
        var totalMinutes = (int)Math.Round((double)hoursDecimal * 60);
        var hrs = totalMinutes / 60;
        var mins = totalMinutes % 60;
        return $"{hrs}:{mins:D2}";
    }
}