using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Infrastructure.Services;
using TravelHub.Web.ViewModels.Activities;

namespace TravelHub.Web.Controllers;

[Authorize]
public class SpotsController : Controller
{
    private readonly ISpotService _spotService;
    private readonly IActivityService _activityService;
    private readonly IGenericService<Category> _categoryService;
    private readonly IActivityService _activityService;
    private readonly ITripService _tripService;
    private readonly IGenericService<Day> _dayService;
    private readonly IPhotoService _photoService;
    private readonly ILogger<SpotsController> _logger;
    private readonly UserManager<Person> _userManager;
    private readonly IConfiguration _configuration;

    public SpotsController(
        ISpotService spotService,
        IActivityService activityService,
        IGenericService<Category> categoryService,
        ITripService tripService,
        IGenericService<Day> dayService,
        IPhotoService photoService,
        ILogger<SpotsController> logger,
        IConfiguration configuration,
        UserManager<Person> userManager)
    {
        _spotService = spotService;
        _activityService = activityService;
        _categoryService = categoryService;
        _tripService = tripService;
        _dayService = dayService;
        _photoService = photoService;
        _logger = logger;
        _configuration = configuration;
        _userManager = userManager;
    }

    // GET: Spots
    public async Task<IActionResult> Index()
    {
        var spots = await _spotService.GetAllWithDetailsAsync();
        var viewModel = spots.Select(s => new SpotDetailsViewModel
        {
            Id = s.Id,
            Name = s.Name,
            Description = s.Description!,
            Duration = s.Duration,
            DurationString = ConvertDecimalToTimeString(s.Duration),
            Order = s.Order,
            CategoryName = s.Category?.Name,
            TripName = s.Trip?.Name!,
            DayName = s.Day?.Name,
            Longitude = s.Longitude,
            Latitude = s.Latitude,
            Cost = s.Cost,
            PhotoCount = s.Photos?.Count ?? 0,
            TransportsFromCount = s.TransportsFrom?.Count ?? 0,
            TransportsToCount = s.TransportsTo?.Count ?? 0
        }).ToList();

        return View(viewModel);
    }

    // GET: Spots/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var spot = await _spotService.GetSpotDetailsAsync(id.Value);
        if (spot == null)
        {
            return NotFound();
        }

        if (!await _spotService.UserOwnsSpotAsync(id.Value, GetCurrentUserId()))
        {
            return Forbid();
        }

        var viewModel = new SpotDetailsViewModel
        {
            Id = spot.Id,
            Name = spot.Name,
            Description = spot.Description!,
            Duration = spot.Duration,
            DurationString = ConvertDecimalToTimeString(spot.Duration),
            Order = spot.Order,
            CategoryName = spot.Category?.Name,
            TripName = spot.Trip?.Name!,
            TripId = spot.TripId,
            DayName = spot.Day?.Name,
            Longitude = spot.Longitude,
            Latitude = spot.Latitude,
            Cost = spot.Cost,
            PhotoCount = spot.Photos?.Count ?? 0,
            TransportsFromCount = spot.TransportsFrom?.Count ?? 0,
            TransportsToCount = spot.TransportsTo?.Count ?? 0
        };

        // pobierz zdjęcia i zamapuj na PhotoViewModel
        var photos = await _photoService.GetBySpotIdAsync(spot.Id);
        viewModel.Photos = photos.Select(p => new PhotoViewModel
        {
            Id = p.Id,
            Name = p.Name,
            Alt = p.Alt
        }).ToList();

        return View(viewModel);
    }

    // GET: Spots/Create
    public async Task<IActionResult> Create()
    {
        var viewModel = await CreateSpotCreateEditViewModel();
        viewModel.DurationString = "01:00";
        viewModel.Order = 0;
        return View(viewModel);
    }

    // POST: Spots/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SpotCreateEditViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            viewModel.Duration = ConvertTimeStringToDecimal(viewModel.DurationString);
            viewModel.Order = await CalculateNextOrder(viewModel.DayId);

            var spot = new Spot
            {
                Name = viewModel.Name,
                Description = viewModel.Description!,
                Duration = viewModel.Duration,
                Order = viewModel.Order,
                CategoryId = viewModel.CategoryId,
                TripId = viewModel.TripId,
                DayId = viewModel.DayId,
                Longitude = viewModel.Longitude,
                Latitude = viewModel.Latitude,
                Cost = viewModel.Cost
            };

            await _spotService.AddAsync(spot);
            return RedirectToAction("Details", "Trips", new { id = viewModel.TripId });
        }

        await PopulateSelectLists(viewModel);
        return View(viewModel);
    }

    // GET: Spots/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var spot = await _spotService.GetByIdAsync(id.Value);
        if (spot == null)
        {
            return NotFound();
        }

        if (!await _spotService.UserOwnsSpotAsync(id.Value, GetCurrentUserId()))
        {
            return Forbid();
        }

        var viewModel = await CreateSpotCreateEditViewModel(spot);
        viewModel.DurationString = ConvertDecimalToTimeString(spot.Duration);
        return View(viewModel);
    }

    // POST: Spots/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SpotCreateEditViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return NotFound();
        }


        if (!await _spotService.UserOwnsSpotAsync(id, GetCurrentUserId()))
        {
            return Forbid();
        }

        if (ModelState.IsValid)
        {
            try
            {
                viewModel.Duration = ConvertTimeStringToDecimal(viewModel.DurationString);

                var existingSpot = await _spotService.GetByIdAsync(id);
                if (existingSpot == null)
                {
                    return NotFound();
                }

                var oldDayId = existingSpot.DayId;

                // Jeśli zmieniono dzień, przelicz Order
                if (existingSpot.DayId != viewModel.DayId)
                {
                    viewModel.Order = await CalculateNextOrder(viewModel.DayId);
                }

                existingSpot.Name = viewModel.Name;
                existingSpot.Description = viewModel.Description!;
                existingSpot.Duration = viewModel.Duration;
                existingSpot.Order = viewModel.Order;
                existingSpot.CategoryId = viewModel.CategoryId;
                existingSpot.TripId = viewModel.TripId;
                existingSpot.DayId = viewModel.DayId;
                existingSpot.Longitude = viewModel.Longitude;
                existingSpot.Latitude = viewModel.Latitude;
                existingSpot.Cost = viewModel.Cost;

                await _spotService.UpdateAsync(existingSpot);

                // Jeśli zmieniono dzień, przelicz Order w starym i nowym dniu
                if (oldDayId != viewModel.DayId)
                {
                    await RecalculateOrdersForBothDays(oldDayId, viewModel.DayId);
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await SpotExists(viewModel.Id))
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

    // GET: Spots/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        if (!await _spotService.UserOwnsSpotAsync(id.Value, GetCurrentUserId()))
        {
            return Forbid();
        }

        var spot = await _spotService.GetByIdAsync(id.Value);
        if (spot == null)
        {
            return NotFound();
        }

        var viewModel = new SpotDetailsViewModel
        {
            Id = spot.Id,
            Name = spot.Name,
            Description = spot.Description!,
            Duration = spot.Duration,
            DurationString = ConvertDecimalToTimeString(spot.Duration),
            Order = spot.Order,
            CategoryName = spot.Category?.Name,
            TripName = spot.Trip?.Name!,
            DayName = spot.Day?.Name,
            Longitude = spot.Longitude,
            Latitude = spot.Latitude,
            Cost = spot.Cost
        };

        return View(viewModel);
    }

    // POST: Spots/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var spot = await _spotService.GetByIdAsync(id);
        if (spot != null)
        {
            var dayId = spot.DayId;
            await _spotService.DeleteAsync(id);

            // Przelicz Order w dniu po usunięciu spotu
            await RecalculateOrderForDay(dayId);

            return RedirectToAction("Details", "Trips", new { id = spot.TripId });
        }
        else
        {
            return NotFound();
        }
    }

    // GET: Spots/AddToTrip/5
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

        var viewModel = new SpotCreateEditViewModel
        {
            TripId = tripId,
            Order = await CalculateNextOrder(dayId),
            DayId = dayId
        };

        await PopulateSelectListsForTrip(viewModel, tripId);

        ViewData["TripName"] = trip.Name;
        ViewData["DayName"] = dayId.HasValue ?
            trip.Days?.FirstOrDefault(d => d.Id == dayId)?.Name : null;
        ViewData["ReturnUrl"] = Url.Action("Details", "Trips", new { id = tripId });
        ViewData["GoogleApiKey"] = _configuration["ApiKeys:GoogleApiKey"];

        (double lat, double lng) = await _tripService.GetMedianCoords(tripId);

        ViewData["Latitude"] = lat;
        ViewData["Longitude"] = lng;

        return View("AddToTrip", viewModel);
    }

    // POST: Spots/AddToTrip/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToTrip(int tripId, SpotCreateEditViewModel viewModel)
    {
        if (tripId != viewModel.TripId)
        {
            return NotFound();
        }

        if (!await _tripService.UserOwnsTripAsync(tripId, GetCurrentUserId()))
        {
            return Forbid();
        }

        if (ModelState.IsValid)
        {
            try
            {
                viewModel.Duration = ConvertTimeStringToDecimal(viewModel.DurationString);
                viewModel.Order = await CalculateNextOrder(viewModel.DayId);

                var spot = new Spot
                {
                    Name = viewModel.Name,
                    Description = viewModel.Description!,
                    Duration = viewModel.Duration,
                    Order = viewModel.Order,
                    CategoryId = viewModel.CategoryId,
                    TripId = viewModel.TripId,
                    DayId = viewModel.DayId,
                    Longitude = viewModel.Longitude,
                    Latitude = viewModel.Latitude,
                    Cost = viewModel.Cost
                };

                await _spotService.AddAsync(spot);

                TempData["SuccessMessage"] = "Spot added successfully!";
                return RedirectToAction("Details", "Trips", new { id = tripId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding spot to trip");
                ModelState.AddModelError("", "An error occurred while adding the spot.");
            }
        }

        await PopulateSelectListsForTrip(viewModel, tripId);
        return View("AddToTrip", viewModel);
    }

    private async Task PopulateSelectListsForTrip(SpotCreateEditViewModel viewModel, int tripId)
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

    private async Task<bool> SpotExists(int id)
    {
        var spot = await _spotService.GetByIdAsync(id);
        return spot != null;
    }

    private async Task<SpotCreateEditViewModel> CreateSpotCreateEditViewModel(Spot? spot = null)
    {
        var viewModel = new SpotCreateEditViewModel();

        if (spot != null)
        {
            viewModel.Id = spot.Id;
            viewModel.Name = spot.Name;
            viewModel.Description = spot.Description;
            viewModel.Duration = spot.Duration;
            viewModel.Order = spot.Order;
            viewModel.CategoryId = spot.CategoryId;
            viewModel.TripId = spot.TripId;
            viewModel.DayId = spot.DayId;
            viewModel.Longitude = spot.Longitude;
            viewModel.Latitude = spot.Latitude;
            viewModel.Cost = spot.Cost;
        }

        await PopulateSelectLists(viewModel);
        return viewModel;
    }

    private async Task PopulateSelectLists(SpotCreateEditViewModel viewModel)
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
    /// Calculates the next Order value for a spot in a specific day
    /// </summary>
    /// <param name="dayId">The day ID (nullable)</param>
    /// <returns>0 if no day is selected, otherwise the highest Order + 1 for the selected day</returns>
    private async Task<int> CalculateNextOrder(int? dayId)
    {
        if (!dayId.HasValue || dayId == 0)
        {
            return 0;
        }

        // Pobierz wszystkie spoty dla danego dnia
        var itemsInDay = await _activityService.GetAllAsync();
        itemsInDay = itemsInDay.Where(a => a.DayId == dayId).ToList();

        if (!itemsInDay.Any())
        {
            return 1; // Pierwszy spot w tym dniu
        }

        // Znajdź najwyższe Order i dodaj 1
        var maxOrder = itemsInDay.Max(a => a.Order);
        return maxOrder + 1;
    }

    /// <summary>
    /// Recalculates Order for all spots in a day to remove gaps
    /// </summary>
    private async Task RecalculateOrderForDay(int? dayId)
    {
        if (!dayId.HasValue || dayId == 0)
            return;

        var spotsInDay = await _activityService.GetAllAsync();
        spotsInDay = spotsInDay
            .Where(s => s.DayId == dayId)
            .OrderBy(s => s.Order)
            .ToList();

        if (!spotsInDay.Any())
            return;

        // Reset orders sequentially starting from 1
        int newOrder = 1;
        foreach (var spot in spotsInDay)
        {
            spot.Order = newOrder++;
            await _activityService.UpdateAsync(spot);
        }
    }

    /// <summary>
    /// Recalculates Order for both old and new days when moving spot between days
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
}