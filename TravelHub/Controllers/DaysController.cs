using Elfie.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using TravelHub.Domain.DTOs;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Infrastructure.Services;
using TravelHub.Web.ViewModels.Accommodations;
using TravelHub.Web.ViewModels.Activities;
using TravelHub.Web.ViewModels.Days;
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
    private readonly IRouteOptimizationService _routeOptimizationService;
    private readonly IConfiguration _configuration;
    private readonly IPdfService _pdfService;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICompositeViewEngine _viewEngine;
    private readonly ITempDataProvider _tempDataProvider;
    private readonly ILogger<DaysController> _logger;

    public DaysController(IDayService dayService,
        ITripService tripService,
        ITripParticipantService tripParticipantService,
        ITransportService transportService,
        ILogger<DaysController> logger,
        UserManager<Person> userManager,
        IRouteOptimizationService routeOptimizationService,
        IConfiguration configuration,
        IActivityService activityService,
        IPdfService pdfService,
        IWebHostEnvironment webHostEnvironment,
        IHttpContextAccessor httpContextAccessor,
        ICompositeViewEngine viewEngine,
        ITempDataProvider tempDataProvider)
    {
        _dayService = dayService;
        _tripService = tripService;
        _tripParticipantService = tripParticipantService;
        _activityService = activityService;
        _logger = logger;
        _userManager = userManager;
        _routeOptimizationService = routeOptimizationService;
        _configuration = configuration;
        _activityService = activityService;
        _pdfService = pdfService;
        _webHostEnvironment = webHostEnvironment;
        _httpContextAccessor = httpContextAccessor;
        _viewEngine = viewEngine;
        _tempDataProvider = tempDataProvider;
    }

    // GET: Days/Details/5
    public async Task<IActionResult> Details(int id, string source = "", string? returnUrl = null)
    {
        var day = await _dayService.GetDayWithDetailsAsync(id);
        if (day == null)
        {
            return NotFound();
        }

        if (source != "public" && !await _tripParticipantService.UserHasAccessToTripAsync(day.TripId, GetCurrentUserId()))
        {
            return Forbid();
        }

        var allTripActivities = await _activityService.GetTripActivitiesWithDetailsAsync(day.TripId);

        // Filtruj tylko aktywności bez przypisanego dnia i bez accommodation
        var availableActivities = allTripActivities
            .Where(a => a.DayId == null && !(a is Accommodation))
            .ToList();

        // Filtruj activities dnia - bez accommodation
        var dayActivitiesWithoutAccommodation = day.Activities
            .Where(a => !(a is Accommodation))
            .ToList();

        var viewModel = new DayDetailsViewModel
        {
            Id = day.Id,
            Number = day.Number,
            Name = day.Name,
            Date = day.Date,
            TripId = day.TripId,
            Trip = day.Trip,
            AccommodationId = day.AccommodationId,
            Accommodation = day.Accommodation,
            Activities = dayActivitiesWithoutAccommodation.Select(a => new ActivityDetailsViewModel
            {
                Id = a.Id,
                Name = a.Name,
                Description = a.Description!,
                Duration = a.Duration,
                DurationString = ConvertDecimalToTimeString(a.Duration),
                Order = a.Order,
                StartTime = a.StartTime,
                StartTimeString = a.StartTime.HasValue ? ConvertDecimalToTimeString(a.StartTime.Value) : null,
                CategoryName = a.Category?.Name,
                TripId = day.TripId,
                TripName = day.Trip?.Name ?? "",
                DayName = day.Name,
                Type = a is Spot ? "Spot" : "Activity"
            }).ToList(),
            AvailableActivities = availableActivities.Select(a => new ActivityDetailsViewModel
            {
                Id = a.Id,
                Name = a.Name,
                Description = a.Description!,
                Duration = a.Duration,
                DurationString = ConvertDecimalToTimeString(a.Duration),
                Order = a.Order,
                StartTime = a.StartTime,
                StartTimeString = a.StartTime.HasValue ? ConvertDecimalToTimeString(a.StartTime.Value) : null,
                CategoryName = a.Category?.Name,
                TripId = day.TripId,
                TripName = day.Trip?.Name ?? "",
                Type = a is Spot ? "Spot" : "Activity"
            }).ToList()
        };
        if (returnUrl != null)
            returnUrl = source == "public" ? returnUrl + "?source=public" : returnUrl;
        ViewData["ReturnUrl"] = returnUrl ?? (source == "public" ? Url.Action("Details", "TripsSearch", new { id = day.TripId }) : Url.Action("Details", "Trips", new { id = day.TripId }));
        await SetTimeConflictViewDate(id);
        return View(viewModel);
    }

    public async Task<IActionResult> MapView(int id, string source = "")
    {
        var day = await _dayService.GetDayWithDetailsAsync(id);
        if (day == null)
        {
            return NotFound();
        }
        var trip = await _tripService.GetTripWithDetailsAsync(day!.TripId);
        if (trip == null)
        {
            return NotFound();
        }

        if (source != "public" && !await _tripParticipantService.UserHasAccessToTripAsync(day.TripId, GetCurrentUserId()))
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
                                StartTime = a.StartTime,
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
                                StartTime = s.StartTime,
                                TripName = s.Trip?.Name ?? string.Empty,
                                Type = "Spot",
                                DayName = s.Day?.Name,
                                Longitude = s.Longitude,
                                Latitude = s.Latitude,
                                // Cost = s.Cost,
                                PhotoCount = s.Photos?.Count ?? 0,
                                TransportsFrom = s.TransportsFrom?.Select(t => new TransportBasicViewModel {
                                    Id = t.Id,
                                    Name = t.Name,
                                    Duration = t.Duration,
                                    FromSpotId = t.FromSpotId,
                                    ToSpotId = t.ToSpotId,
                                }).ToList(),
                            }).ToList(),
            PreviousAccommodation = BuildAccommodationBasicViewModel(trip.Days!.Where(d => d.Number == day!.Number - 1).FirstOrDefault()),
            NextAccommodation = BuildAccommodationBasicViewModel(day)

        };

        if (viewModel.PreviousAccommodation != null )
        {
            viewModel.Spots.Insert(0, viewModel.PreviousAccommodation);
        }

        if (viewModel.NextAccommodation != null)
        {
            viewModel.NextAccommodation.Order = viewModel.Spots.Count + 1; 
            viewModel.Spots.Add(viewModel.NextAccommodation);
        }

        ViewData["GoogleApiKey"] = _configuration["ApiKeys:GoogleApiKey"];

        (double lat, double lng) = await _dayService.GetMedianCoords(id);

        ViewData["Latitude"] = lat;
        ViewData["Longitude"] = lng;

        return View(viewModel);
    }

    private AccommodationBasicViewModel? BuildAccommodationBasicViewModel(Day? day)
    {
        if (day == null || day.Accommodation == null) { return null; }
        Accommodation a = day.Accommodation;
        return new AccommodationBasicViewModel
        {
            Id = a.Id,
            Name = a.Name,
            Description = a.Description ?? string.Empty,
            Duration = a.Duration,
            DurationString = ConvertDecimalToTimeString(a.Duration),
            Order = a.Order,
            CategoryName = a.Category?.Name,
            TripName = a.Trip?.Name ?? string.Empty,
            Type = "Spot",
            TransportsFrom = a.TransportsFrom?.Select(t => new TransportBasicViewModel
            {
                Id = t.Id,
                Name = t.Name,
                Duration = t.Duration,
                FromSpotId = t.FromSpotId,
                ToSpotId = t.ToSpotId,
            }).ToList(),
            DayName = a.Day?.Name,
            CheckIn = a.CheckIn,
            CheckInTime = a.CheckInTime,
            CheckOut = a.CheckOut,
            CheckOutTime = a.CheckOutTime,
            Latitude = a.Latitude,
            Longitude = a.Longitude,
        };
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

    public async Task<IActionResult> RouteOptimization(int id, int? fixedFirst, int? fixedLast, double startTime = 8, string travelMode = "WALK")
    {
        Day? day = await _dayService.GetDayWithDetailsAsync(id);
        if (day == null)
        {
            return NotFound();
        }

        var trip = await _tripService.GetTripWithDetailsAsync(day!.TripId);
        if (trip == null)
        {
            return NotFound();
        }

        Day? previousDay = trip.Days!.Where(d => d.Number == day!.Number - 1).FirstOrDefault();

        Spot? firstSpot = null;
        if (fixedFirst != null)
        {
            firstSpot = day.Activities.Where(a => a is Spot && a.Id == fixedFirst).Cast<Spot>().FirstOrDefault();
        }
        if (firstSpot == null && previousDay != null)
        {
            firstSpot = previousDay?.Accommodation;
        }

        Spot? lastSpot = null;
        if (fixedLast != null)
        {
            lastSpot = day.Activities.Where(a => a is Spot && a.Id == fixedLast).Cast<Spot>().FirstOrDefault();
        }
        if (lastSpot == null)
        {
            lastSpot = day.Accommodation;
        }

        List<RouteOptimizationSpot> spots = day.Activities.Where(a => a is Spot).Where(a => a.Id != fixedFirst && a.Id != fixedLast).Cast<Spot>().Select(a => new RouteOptimizationSpot
                                                                                                                {
                                                                                                                    Id = a.Id,
                                                                                                                    StartTime = a.StartTime,
                                                                                                                    Duration = a.Duration,
                                                                                                                    Order = a.Order,
                                                                                                                    Type = "Spot",
                                                                                                                    Latitude = a.Latitude,
                                                                                                                    Longitude = a.Longitude,
                                                                                                                }).ToList();

        List<RouteOptimizationActivity> otherActivities = day.Activities.Where(a => a is not Spot).Select(a => new RouteOptimizationActivity
                                                                                                                {
                                                                                                                    Id = a.Id,
                                                                                                                    StartTime = a.StartTime,
                                                                                                                    Duration = a.Duration,
                                                                                                                    Order = a.Order,
                                                                                                                    Type = "Activity"
                                                                                                                }).ToList();

        List<RouteOptimizationTransport> transports = trip.Transports.Select(t => new RouteOptimizationTransport
                                                                            {
                                                                                FromSpotId = t.FromSpotId,
                                                                                ToSpotId = t.ToSpotId,
                                                                                Duration = t.Duration,
                                                                            }).ToList();
        

        RouteOptimizationSpot? first = firstSpot == null ? null : new RouteOptimizationSpot
        {
            Id = firstSpot.Id,
            StartTime = firstSpot.StartTime,
            Duration = firstSpot.Duration,
            Order = firstSpot.Order,
            Type = "Spot",
            Latitude = firstSpot.Latitude,
            Longitude = firstSpot.Longitude,
        };
        RouteOptimizationSpot? end = lastSpot == null ? null : new RouteOptimizationSpot
        {
            Id = lastSpot.Id,
            StartTime = lastSpot.StartTime,
            Duration = lastSpot.Duration,
            Order = lastSpot.Order,
            Type = "Spot",
            Latitude = lastSpot.Latitude,
            Longitude = lastSpot.Longitude,
        };

        if (spots.Count < 2)
        {
            return NotFound();
        }

        List<ActivityOrder> result = await _routeOptimizationService.GetActivityOrderSuggestion(spots, otherActivities, first, end, transports, travelMode, (decimal)startTime); 

        return Ok(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateActivityOrder([FromBody] UpdateActivityOrderRequest request)
    {
        var day = await _dayService.GetDayByIdAsync(request.DayId);

        if (!await _tripParticipantService.UserHasAccessToTripAsync(day!.TripId, GetCurrentUserId()))
        {
            return Forbid();
        }

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

    public async Task<IActionResult> CheckNewForCollisions(int id, string startTimeString, string? durationString)
    {
        Activity? collisionWith = await _dayService.CheckNewForCollisions(id, startTimeString, durationString);
        if (collisionWith == null)
        {
            return Ok(new { collision = false });
        } else
        {
            return Ok(new
            {
                collision = true,
                name = collisionWith.Name,
                startTimeString = ConvertDecimalToTimeString(collisionWith.StartTime!.Value),
                endTimeString = collisionWith.Duration > 0 ? ConvertDecimalToTimeString((collisionWith.StartTime!.Value + collisionWith.Duration) % 24) : null
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExportDayPdf(int id, string? routeData = null)
    {
        var day = await _dayService.GetDayWithDetailsAsync(id);
        if (day == null)
            return NotFound();

        if (!await _tripParticipantService.UserHasAccessToTripAsync(day.TripId, GetCurrentUserId()))
        {
            return Forbid();
        }

        // Pobierz szczegóły dnia z widoku mapy
        var trip = await _tripService.GetTripWithDetailsAsync(day.TripId);

        var viewModel = new DayExportPdfViewModel
        {
            Id = day.Id,
            Number = day.Number,
            Name = day.Name ?? string.Empty,
            Date = day.Date,
            TripName = trip?.Name ?? string.Empty,
            TripStartDate = trip?.StartDate,
            TripEndDate = trip?.EndDate,

            Activities = day.Activities
                .Where(a => !(a is Spot))
                .OrderBy(a => a.Order)
                .Select(a => new ActivityExportViewModel
                {
                    Id = a.Id,
                    Name = a.Name,
                    Description = a.Description,
                    Duration = a.Duration,
                    DurationString = ConvertDecimalToTimeString(a.Duration),
                    Order = a.Order,
                    StartTime = a.StartTime,
                    StartTimeString = a.StartTime.HasValue ? ConvertDecimalToTimeString(a.StartTime.Value) : null,
                    CategoryName = a.Category?.Name,
                    Type = "Activity",
                    Checklist = a.Checklist
                }).ToList(),

            Spots = day.Activities
                .Where(a => a is Spot && !(a is Accommodation))
                .OrderBy(a => a.Order)
                .Cast<Spot>()
                .Select(s => new SpotExportViewModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    Duration = s.Duration,
                    DurationString = ConvertDecimalToTimeString(s.Duration),
                    Order = s.Order,
                    StartTime = s.StartTime,
                    StartTimeString = s.StartTime.HasValue ? ConvertDecimalToTimeString(s.StartTime.Value) : null,
                    CategoryName = s.Category?.Name,
                    Latitude = s.Latitude, // double z encji
                    Longitude = s.Longitude, // double z encji
                    PhotoCount = s.Photos?.Count ?? 0,
                    Checklist = s.Checklist
                }).ToList(),

            Accommodation = day.Accommodation != null ? new AccommodationExportViewModel
            {
                Id = day.Accommodation.Id,
                Name = day.Accommodation.Name,
                Description = day.Accommodation.Description,
                CheckIn = day.Accommodation.CheckIn,
                CheckOut = day.Accommodation.CheckOut,
                CheckInTime = day.Accommodation.CheckInTime,
                CheckOutTime = day.Accommodation.CheckOutTime,
                Latitude = day.Accommodation.Latitude, // double z encji
                Longitude = day.Accommodation.Longitude, // double z encji
                Checklist = day.Accommodation.Checklist
            } : null
        };

        // Dodaj poprzednie zakwaterowanie jeśli istnieje
        if (trip?.Days != null && day.Number.HasValue && day.Number > 1)
        {
            var previousDay = trip.Days.FirstOrDefault(d => d.Number == day.Number - 1);
            if (previousDay?.Accommodation != null)
            {
                viewModel.PreviousAccommodation = new AccommodationExportViewModel
                {
                    Id = previousDay.Accommodation.Id,
                    Name = previousDay.Accommodation.Name,
                    Latitude = previousDay.Accommodation.Latitude,
                    Longitude = previousDay.Accommodation.Longitude
                };
            }
        }

        // Przetwarzanie danych trasy z widoku mapy
        if (!string.IsNullOrEmpty(routeData))
        {
            try
            {
                viewModel.RouteData = JsonConvert.DeserializeObject<RouteDataViewModel>(routeData);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not parse route data for day {DayId}", id);
            }
        }

        // Oblicz środek mapy dla wszystkich punktów (użyj double zamiast decimal)
        var allPoints = new List<(double Latitude, double Longitude)>();

        if (viewModel.PreviousAccommodation != null)
        {
            allPoints.Add((viewModel.PreviousAccommodation.Latitude, viewModel.PreviousAccommodation.Longitude));
        }

        allPoints.AddRange(viewModel.Spots.Select(s => (s.Latitude, s.Longitude)));

        if (viewModel.Accommodation != null)
        {
            allPoints.Add((viewModel.Accommodation.Latitude, viewModel.Accommodation.Longitude));
        }

        if (allPoints.Any())
        {
            viewModel.MapCenterLat = allPoints.Average(p => p.Latitude);
            viewModel.MapCenterLng = allPoints.Average(p => p.Longitude);

            // Oblicz zoom na podstawie zakresu współrzędnych
            if (allPoints.Count > 1)
            {
                var minLat = allPoints.Min(p => p.Latitude);
                var maxLat = allPoints.Max(p => p.Latitude);
                var minLng = allPoints.Min(p => p.Longitude);
                var maxLng = allPoints.Max(p => p.Longitude);

                var latRange = maxLat - minLat;
                var lngRange = maxLng - minLng;
                var maxRange = Math.Max(latRange, lngRange);

                // Oblicz przybliżony zoom (logarytmicznie odwrotnie proporcjonalny do zakresu)
                viewModel.MapZoom = Math.Max(10, Math.Min(15, 15 - Math.Log10(maxRange * 100)));
            }
        }

        // Generuj URL mapy statycznej
        var staticMapUrl = await GenerateStaticMapUrl(viewModel);
        viewModel.StaticMapUrl = staticMapUrl;

        // Renderuj widok PDF
        var htmlString = await RenderViewToStringAsync("DayPdf", viewModel);
        var fileName = day.Number.HasValue
            ? $"Day_{day.Number}_{day.Date:yyyy-MM-dd}.pdf"
            : $"Group_{day.Name}_{day.Date:yyyy-MM-dd}.pdf";

        var bytes = await _pdfService.GeneratePdfFromHtmlAsync(htmlString, fileName);

        return File(bytes, "application/pdf", fileName);
    }

    private async Task<string> RenderViewToStringAsync(string viewName, object model)
    {
        var actionContext = new ActionContext(_httpContextAccessor.HttpContext!, this.RouteData, this.ControllerContext.ActionDescriptor);

        using var sw = new StringWriter();

        var viewResult = _viewEngine.FindView(actionContext, viewName, false);
        if (!viewResult.Success)
            throw new InvalidOperationException($"Could not find view {viewName}");

        var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        {
            Model = model
        };

        var viewContext = new ViewContext(
            actionContext,
            viewResult.View,
            viewDictionary,
            new TempDataDictionary(actionContext.HttpContext, _tempDataProvider),
            sw,
            new HtmlHelperOptions()
        );

        await viewResult.View.RenderAsync(viewContext);

        return sw.ToString();
    }

    private async Task<string> GenerateStaticMapUrl(DayExportPdfViewModel model)
    {
        var apiKey = _configuration["ApiKeys:GoogleApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("Google Maps API key is not configured");
            return string.Empty;
        }

        var parameters = new List<string>
    {
        $"size=800x500",
        $"scale=2", // Większa rozdzielczość dla PDF
        $"maptype=roadmap",
        $"key={apiKey}"
    };

        // Środek mapy
        parameters.Add($"center={model.MapCenterLat:F6},{model.MapCenterLng:F6}");

        // Zoom - jeśli mamy trasę, ustawiamy odpowiedni zoom
        if (model.HasRoute && model.RouteData != null)
        {
            // Oblicz zoom na podstawie bounding box
            var allPoints = GetAllPoints(model);
            if (allPoints.Any())
            {
                var zoom = CalculateOptimalZoom(allPoints);
                parameters.Add($"zoom={zoom}");
            }
            else
            {
                parameters.Add($"zoom={model.MapZoom}");
            }
        }
        else
        {
            parameters.Add($"zoom={model.MapZoom}");
        }

        // Dodaj markery
        var markers = GenerateMarkers(model);
        if (!string.IsNullOrEmpty(markers))
        {
            parameters.Add(markers);
        }

        // Dodaj ścieżkę trasy jeśli istnieje
        if (model.HasRoute && model.RouteData != null)
        {
            var path = GenerateRoutePath(model.RouteData);
            if (!string.IsNullOrEmpty(path))
            {
                parameters.Add(path);
            }
        }

        return $"https://maps.googleapis.com/maps/api/staticmap?{string.Join("&", parameters)}";
    }

    private List<(double Lat, double Lng)> GetAllPoints(DayExportPdfViewModel model)
    {
        var points = new List<(double Lat, double Lng)>();

        if (model.PreviousAccommodation != null)
        {
            points.Add((model.PreviousAccommodation.Latitude, model.PreviousAccommodation.Longitude));
        }

        points.AddRange(model.Spots.Select(s => (s.Latitude, s.Longitude)));

        if (model.Accommodation != null)
        {
            points.Add((model.Accommodation.Latitude, model.Accommodation.Longitude));
        }

        return points;
    }

    private int CalculateOptimalZoom(List<(double Lat, double Lng)> points)
    {
        if (points.Count < 2) return 13;

        var minLat = points.Min(p => p.Lat);
        var maxLat = points.Max(p => p.Lat);
        var minLng = points.Min(p => p.Lng);
        var maxLng = points.Max(p => p.Lng);

        var latDiff = maxLat - minLat;
        var lngDiff = maxLng - minLng;
        var maxDiff = Math.Max(latDiff, lngDiff);

        // Algorytm do obliczania zoom na podstawie zakresu współrzędnych
        if (maxDiff < 0.01) return 15;
        if (maxDiff < 0.02) return 14;
        if (maxDiff < 0.04) return 13;
        if (maxDiff < 0.08) return 12;
        if (maxDiff < 0.16) return 11;
        if (maxDiff < 0.32) return 10;
        if (maxDiff < 0.64) return 9;
        return 8;
    }

    private string GenerateMarkers(DayExportPdfViewModel model)
    {
        var markers = new List<string>();

        // Marker dla poprzedniego zakwaterowania (żółty)
        if (model.PreviousAccommodation != null)
        {
            markers.Add($"color:0xFFC107|label:P|{model.PreviousAccommodation.Latitude:F6},{model.PreviousAccommodation.Longitude:F6}");
        }

        // Markery dla spotów (zielone z numerami)
        int spotNumber = 1;
        foreach (var spot in model.Spots.OrderBy(s => s.Order))
        {
            markers.Add($"color:0x28a745|label:{spotNumber}|{spot.Latitude:F6},{spot.Longitude:F6}");
            spotNumber++;
        }

        // Marker dla aktualnego zakwaterowania (pomarańczowy)
        if (model.Accommodation != null)
        {
            markers.Add($"color:0xFD7E14|label:A|{model.Accommodation.Latitude:F6},{model.Accommodation.Longitude:F6}");
        }

        if (!markers.Any()) return string.Empty;

        return $"markers={string.Join("&markers=", markers)}";
    }

    private string GenerateRoutePath(RouteDataViewModel routeData)
    {
        if (routeData?.Waypoints == null || routeData.Waypoints.Count < 2)
            return string.Empty;

        // Sortuj waypoints po kolejności
        var sortedWaypoints = routeData.Waypoints
            .OrderBy(w => w.Order)
            .ToList();

        // Tworzymy ścieżkę z punktów trasy
        var pathPoints = new List<string>();
        foreach (var point in sortedWaypoints)
        {
            pathPoints.Add($"{point.Latitude:F6},{point.Longitude:F6}");
        }

        if (pathPoints.Count < 2) return string.Empty;

        return $"path=color:0x667eea|weight:4|fillcolor:0x667eea20|{string.Join("|", pathPoints)}";
    }

    private string GetCurrentUserId()
    {
        return _userManager.GetUserId(User) ?? throw new UnauthorizedAccessException("User is not authenticated");
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

    private async Task SetTimeConflictViewDate(int id)
    {
        (Activity, Activity)? conflict = await _dayService.CheckAllForCollisions(id);
        if (conflict != null)
        {
            (Activity one, Activity other) = conflict.Value;
            ViewData["Conflict"] = true;
            ViewData["ConflictAName"] = one.Name;
            ViewData["ConflictBName"] = other.Name;
            ViewData["ConflictATimeString"] = $"{ConvertDecimalToTimeString(one.StartTime!.Value)} {(one.Duration > 0 ? "- " + ConvertDecimalToTimeString(one.StartTime!.Value + one.Duration) : "")}";
            ViewData["ConflictBTimeString"] = $"{ConvertDecimalToTimeString(other.StartTime!.Value)} {(other.Duration > 0 ? "- " + ConvertDecimalToTimeString((other.StartTime!.Value + other.Duration) % 24) : "")}";
        } else
        {
            ViewData["Conflict"] = false;
        }
    }
}
