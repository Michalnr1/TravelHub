using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Web.ViewModels.Activities;
using TravelHub.Web.ViewModels.Transports;
using TravelHub.Web.ViewModels.Trips;

namespace TravelHub.Web.Controllers;

[Authorize]
public class DaysController : Controller
{
    private readonly IDayService _dayService;
    private readonly ITripService _tripService;
    private readonly ITripParticipantService _tripParticipantService;
    private readonly IActivityService _activityService;
    private readonly UserManager<Person> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DaysController> _logger;

    public DaysController(IDayService dayService,
        ITripService tripService,
        ITripParticipantService tripParticipantService,
        ILogger<DaysController> logger,
        UserManager<Person> userManager,
        IConfiguration configuration,
        IActivityService activityService)
    {
        _dayService = dayService;
        _tripService = tripService;
        _tripParticipantService = tripParticipantService;
        _logger = logger;
        _userManager = userManager;
        _configuration = configuration;
        _activityService = activityService;
    }

    // GET: Days/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var day = await _dayService.GetDayWithDetailsAsync(id);
        if (day == null)
        {
            return NotFound();
        }

        return View(day);
    }

    public async Task<IActionResult> MapView(int id)
    {
        var day = await _dayService.GetDayWithDetailsAsync(id);
        var trip = await _tripService.GetTripWithDetailsAsync(day!.TripId);
        if (day == null)
        {
            return NotFound();
        }

        if (!await _tripParticipantService.UserHasAccessToTripAsync(day.TripId, GetCurrentUserId()))
        {
            return Forbid();
        }

        var viewModel = new DayDetailViewModel
        {
            Id = day.Id,
            Number = day.Number,
            Name = day.Name,
            Date = day.Date,
            Trip = new TripViewModel
            {
                Id = day.TripId,
                Name = trip!.Name,
                Status = trip.Status,
                StartDate = trip.StartDate,
                EndDate = trip.EndDate,
                IsPrivate = trip.IsPrivate,
                DaysCount = trip.Days?.Count ?? 0,
                GroupsCount = (trip.Days ?? Enumerable.Empty<Day>()).Where(d => !d.Number.HasValue).Count()
            },
            Activities = day.Activities
                            .Where(a => a is not Spot)
                            .OrderBy(a => a.Order)
                            .Select(a => new ActivityDetailsViewModel
                            {
                                Id = a.Id,
                                Name = a.Name,
                                Description = a.Description ?? string.Empty,
                                Duration = a.Duration,
                                DurationString = ConvertDecimalToTimeString(a.Duration),
                                Order = a.Order,
                                CategoryName = a.Category?.Name,
                                TripName = a.Trip?.Name ?? string.Empty,
                                TripId = a.TripId,
                                Type = "Activity",
                                DayName = a.Day?.Name
                            }).ToList(),
            Spots = day.Activities
                            .Where(a => a is Spot)
                            .OrderBy(a => a.Order)
                            .Cast<Spot>()
                            .Select(s => new SpotDetailsViewModel
                            {
                                Id = s.Id,
                                Name = s.Name,
                                Description = s.Description ?? string.Empty,
                                Duration = s.Duration,
                                DurationString = ConvertDecimalToTimeString(s.Duration),
                                Order = s.Order,
                                CategoryName = s.Category?.Name,
                                TripName = s.Trip?.Name ?? string.Empty,
                                Type = "Spot",
                                DayName = s.Day?.Name,
                                Longitude = s.Longitude,
                                Latitude = s.Latitude,
                                // Cost = s.Cost,
                                PhotoCount = s.Photos?.Count ?? 0,
                                TransportsFromCount = s.TransportsFrom?.Count ?? 0,
                                TransportsToCount = s.TransportsTo?.Count ?? 0,
                                TransportsFrom = s.TransportsFrom?.Select(t => new TransportBasicViewModel { 
                                    Name = t.Name,
                                    Duration = t.Duration,
                                    FromSpotId = t.FromSpotId,
                                    ToSpotId = t.ToSpotId,
                                }).ToList(),
                            }).ToList(),


        };

        ViewData["GoogleApiKey"] = _configuration["ApiKeys:GoogleApiKey"];

        (double lat, double lng) = await _dayService.GetMedianCoords(id);

        ViewData["Latitude"] = lat;
        ViewData["Longitude"] = lng;

        return View(viewModel);
    }

    // GET: Days/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var day = await _dayService.GetByIdAsync(id);
        if (day == null)
        {
            return NotFound();
        }

        if (!await _tripParticipantService.UserHasAccessToTripAsync(day.TripId, GetCurrentUserId()))
        {
            return Forbid();
        }

        var viewModel = new DayViewModel
        {
            Id = day.Id,
            Number = day.Number,
            Name = day.Name,
            Date = day.Date
        };

        return View(viewModel);
    }

    // POST: Days/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DayViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return NotFound();
        }

        var day = await _dayService.GetByIdAsync(id);
        if (day == null)
        {
            return NotFound();
        }

        if (!await _tripParticipantService.UserHasAccessToTripAsync(day.TripId, GetCurrentUserId()))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        try
        {
            day.Number = viewModel.Number;
            day.Name = viewModel.Name;
            day.Date = viewModel.Date;

            await _dayService.UpdateAsync(day);

            TempData["SuccessMessage"] = "Day updated successfully!";
            return RedirectToAction(nameof(Details), new { id = day.Id });
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogError("Concurrency error while updating day {Id}", id);
            ModelState.AddModelError("", "Error updating the day, please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating day {Id}", id);
            ModelState.AddModelError("", "An unexpected error occurred.");
        }

        return View(viewModel);
    }

    // GET: Days/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var day = await _dayService.GetByIdAsync(id);
        if (day == null)
        {
            return NotFound();
        }

        if (!await _tripParticipantService.UserHasAccessToTripAsync(day.TripId, GetCurrentUserId()))
        {
            return Forbid();
        }

        return View(day);
    }

    // POST: Days/Delete/5
    [HttpPost, ActionName("DeleteConfirmed")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var day = await _dayService.GetDayByIdAsync(id);
        if (day == null)
        {
            return NotFound();
        }

        if (!await _tripParticipantService.UserHasAccessToTripAsync(day.TripId, GetCurrentUserId()))
        {
            return Forbid();
        }

        try
        {
            var tripId = day.TripId;
            await _dayService.DeleteAsync(id);
            TempData["SuccessMessage"] = "Day deleted successfully!";
            return RedirectToAction("Details", "Trips", new { id = tripId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting day {Id}", id);
            ModelState.AddModelError("", "Error deleting the day.");
            return RedirectToAction(nameof(Delete), new { id });
        }
    }

    // GET: Day/EditGroup/5
    [HttpGet]
    public async Task<IActionResult> EditGroup(int id)
    {
        var day = await _dayService.GetDayByIdAsync(id);
        if (day == null)
        {
            return NotFound();
        }

        if (!await _tripParticipantService.UserHasAccessToTripAsync(day.TripId, GetCurrentUserId()))
        {
            return Forbid();
        }

        // Sprawdź czy dzień jest grupą
        if (!await _dayService.IsDayAGroupAsync(id))
        {
            return BadRequest("This day is not a group.");
        }

        if (!await _dayService.UserOwnsDayAsync(id, GetCurrentUserId()))
        {
            return Forbid();
        }

        var viewModel = new EditDayViewModel
        {
            Id = day.Id,
            TripId = day.TripId,
            Name = day.Name,
            Date = day.Date,
            IsGroup = true
        };

        var trip = await _tripService.GetByIdAsync(day.TripId);
        if (trip != null)
        {
            viewModel.TripName = trip.Name;
            viewModel.MinDate = trip.StartDate;
            viewModel.MaxDate = trip.EndDate;
        }

        ViewData["FormTitle"] = "Edit Group";
        return View(viewModel);
    }

    // POST: Day/EditGroup/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditGroup(int id, EditDayViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return NotFound();
        }

        if (!await _tripParticipantService.UserHasAccessToTripAsync(viewModel.TripId, GetCurrentUserId()))
        {
            return Forbid();
        }

        // Ustaw IsGroup na true i upewnij się, że Number jest null
        viewModel.IsGroup = true;
        viewModel.Number = null;

        var existingDay = await _dayService.GetDayByIdAsync(id);
        if (existingDay == null)
        {
            return NotFound();
        }

        if (!await _dayService.UserOwnsDayAsync(id, GetCurrentUserId()))
        {
            return Forbid();
        }

        // Sprawdź czy dzień jest grupą
        if (!await _dayService.IsDayAGroupAsync(id))
        {
            ModelState.AddModelError("", "This day is not a group.");
        }

        // Walidacja: Nazwa jest wymagana dla Grupy
        if (string.IsNullOrWhiteSpace(viewModel.Name))
        {
            ModelState.AddModelError(nameof(viewModel.Name), "Group name is required.");
        }

        // Walidacja zakresu daty
        //if (viewModel.Date.HasValue &&
        //    !await _dayService.ValidateDateRangeAsync(existingDay.TripId, viewModel.Date.Value))
        //{
        //    ModelState.AddModelError(nameof(viewModel.Date), "Date must be within the trip date range.");
        //}

        if (ModelState.IsValid)
        {
            try
            {
                existingDay.Name = viewModel.Name;
                existingDay.Date = viewModel.Date;

                await _dayService.UpdateAsync(existingDay);

                TempData["SuccessMessage"] = "Group updated successfully!";
                return RedirectToAction("Details", "Trips", new { id = existingDay.TripId });
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating group");
                ModelState.AddModelError("", "An error occurred while updating the group.");
            }
        }

        // Ponownie ustaw właściwości potrzebne dla widoku
        var trip = await _tripService.GetByIdAsync(existingDay.TripId);
        if (trip != null)
        {
            viewModel.TripName = trip.Name;
            viewModel.MinDate = trip.StartDate;
            viewModel.MaxDate = trip.EndDate;
        }

        ViewData["FormTitle"] = "Edit Group";
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateActivityOrder([FromBody] UpdateActivityOrderRequest request)
    {
        try
        {
            if (request?.Activities == null || !request.Activities.Any())
            {
                return Json(new { success = false, message = "No activities provided" });
            }

            foreach (var activityOrder in request.Activities)
            {
                var activity = await _activityService.GetByIdAsync(activityOrder.ActivityId);
                if (activity != null && activity.DayId == request.DayId)
                {
                    activity.Order = activityOrder.Order;
                    await _activityService.UpdateAsync(activity);
                }
            }

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating activity order for day {DayId}", request?.DayId);
            return Json(new { success = false, message = "Error updating order" });
        }
    }

    public class UpdateActivityOrderRequest
    {
        public int DayId { get; set; }
        public List<ActivityOrder> Activities { get; set; } = new();
    }

    public class ActivityOrder
    {
        public int ActivityId { get; set; }
        public int Order { get; set; }
    }

    private string GetCurrentUserId()
    {
        return _userManager.GetUserId(User) ?? throw new UnauthorizedAccessException("User is not authenticated");
    }

    private string ConvertDecimalToTimeString(decimal duration)
    {
        int hours = (int)duration;
        int minutes = (int)((duration - hours) * 60);
        return $"{hours:D2}:{minutes:D2}";
    }
}
