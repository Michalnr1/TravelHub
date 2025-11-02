using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Infrastructure.Services;
using TravelHub.Web.ViewModels.Activities;
using TravelHub.Web.ViewModels.Expenses;
using System.Text;
using System.Globalization;
using CategorySelectItem = TravelHub.Web.ViewModels.Activities.CategorySelectItem;

namespace TravelHub.Web.Controllers;

[Authorize]
public class SpotsController : Controller
{
    private readonly ISpotService _spotService;
    private readonly IActivityService _activityService;
    private readonly IGenericService<Category> _categoryService;
    private readonly ITripService _tripService;
    private readonly ITripParticipantService _tripParticipantService;
    private readonly IGenericService<Day> _dayService;
    private readonly IPhotoService _photoService;
    private readonly IReverseGeocodingService _reverseGeocodingService;
    private readonly IExpenseService _expenseService;
    private readonly IExchangeRateService _exchangeRateService;
    private readonly ILogger<SpotsController> _logger;
    private readonly UserManager<Person> _userManager;
    private readonly IConfiguration _configuration;

    public SpotsController(
        ISpotService spotService,
        IActivityService activityService,
        IGenericService<Category> categoryService,
        ITripService tripService,
        ITripParticipantService tripParticipantService,
        IGenericService<Day> dayService,
        IPhotoService photoService,
        IReverseGeocodingService reverseGeocodingService,
        IExpenseService expenseService,
        IExchangeRateService exchangeRateService,
        ILogger<SpotsController> logger,
        IConfiguration configuration,
        UserManager<Person> userManager)
    {
        _spotService = spotService;
        _activityService = activityService;
        _categoryService = categoryService;
        _tripService = tripService;
        _tripParticipantService = tripParticipantService;
        _dayService = dayService;
        _photoService = photoService;
        _reverseGeocodingService = reverseGeocodingService;
        _expenseService = expenseService;
        _exchangeRateService = exchangeRateService;
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
            // Cost = s.Cost,
            Rating = s.Rating,
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
            // Cost = spot.Cost,
            Rating = spot.Rating,
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
                // Cost = viewModel.Cost,
                Rating = viewModel.Rating
            };

            var createdSpot = await _spotService.AddAsync(spot);

            // Jeśli podano koszt, utwórz powiązany Expense
            if (viewModel.ExpenseValue.HasValue && viewModel.ExpenseValue > 0)
            {
                await CreateExpenseForSpot(createdSpot, viewModel);
            }

            return RedirectToAction("Details", "Trips", new { id = viewModel.TripId });
        }

        await PopulateSelectLists(viewModel, viewModel.TripId);
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
                // existingSpot.Cost = viewModel.Cost;
                existingSpot.Rating = viewModel.Rating;

                await _spotService.UpdateAsync(existingSpot);

                // Jeśli zmieniono dzień, przelicz Order w starym i nowym dniu
                if (oldDayId != viewModel.DayId)
                {
                    await RecalculateOrdersForBothDays(oldDayId, viewModel.DayId);
                }

                // Aktualizacja Expense
                await UpdateExpenseForSpot(existingSpot, viewModel);
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

        await PopulateSelectLists(viewModel, viewModel.TripId);
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
            // Cost = spot.Cost,
            Rating = spot.Rating
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

        await SetAddToDayViewData(trip, dayId);

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

        var trip = await _tripService.GetByIdAsync(tripId);
        if (trip == null)
        {
            return NotFound();
        }

        if (!await _tripParticipantService.UserHasAccessToTripAsync(tripId, GetCurrentUserId()))
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
                    // Cost = viewModel.Cost,
                    Rating = viewModel.Rating
                };

                var createdSpot = await _spotService.AddAsync(spot);

                // Jeśli podano koszt, utwórz powiązany Expense
                if (viewModel.ExpenseValue.HasValue && viewModel.ExpenseValue > 0)
                {
                    await CreateExpenseForSpot(createdSpot, viewModel);
                }

                (string? countryName, string? countryCode, string? city) = await _reverseGeocodingService.GetCountryAndCity(spot.Latitude, spot.Longitude);
                if (countryName != null && countryCode != null)
                {
                    await _tripService.AddCountryToTrip(tripId, countryName, countryCode);
                }

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
        await SetAddToDayViewData(trip, viewModel.DayId);
        return View("AddToTrip", viewModel);
    }

    private async Task SetAddToDayViewData(Trip trip, int? dayId)
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
        ViewData["GoogleApiKey"] = _configuration["ApiKeys:GoogleApiKey"];

        (double lat, double lng) = await _tripService.GetMedianCoords(trip.Id);

        ViewData["Latitude"] = lat;
        ViewData["Longitude"] = lng;
    }

    // GET: Spots/Export/5
    public async Task<IActionResult> Export(int? id)
    {
        if (id == null) return NotFound();

        var spot = await _spotService.GetByIdAsync(id.Value);
        if (spot == null) return NotFound();

        string name = spot.Name ?? string.Empty;
        string description = spot.Description ?? string.Empty;
        string duration = FormatDuration(spot.Duration);

        var sb = new StringBuilder();
        sb.AppendLine("Spot name:");
        sb.AppendLine(name);
        sb.AppendLine();
        sb.AppendLine("Description:");
        sb.AppendLine(description);
        sb.AppendLine();
        sb.AppendLine("Duration:");
        sb.AppendLine(duration);

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"spot_{spot.Name}.txt";
        return File(bytes, "text/plain; charset=utf-8", fileName);
    }

    private static string FormatDuration(decimal hoursDecimal)
    {
        var totalMinutes = (int)Math.Round((double)hoursDecimal * 60);
        var hrs = totalMinutes / 60;
        var mins = totalMinutes % 60;
        return $"{hrs}:{mins:D2}";
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

        // Currencies dla Expense
        var usedRates = await _exchangeRateService.GetTripExchangeRatesAsync(tripId);
        await PopulateCurrencySelectList(viewModel, usedRates);
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
            // viewModel.Cost = spot.Cost;
            viewModel.Rating = spot.Rating;
        }

        await PopulateSelectLists(viewModel, viewModel.TripId);
        return viewModel;
    }

    private async Task CreateExpenseForSpot(Spot spot, SpotCreateEditViewModel viewModel)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            _logger.LogWarning("Cannot create expense for spot: User not found");
            return;
        }

        try
        {
            // Pobierz lub utwórz exchange rate
            var exchangeRateEntry = await _exchangeRateService
                .GetOrCreateExchangeRateAsync(
                    spot.TripId,
                    viewModel.ExpenseCurrencyCode ?? CurrencyCode.PLN,
                    viewModel.ExpenseExchangeRateValue ?? 1.0M);

            // Utwórz Expense
            var expense = new Expense
            {
                Name = $"{spot.Name} (Expense)",
                Value = viewModel.ExpenseValue!.Value,
                PaidById = currentUser.Id,
                CategoryId = spot.CategoryId,
                ExchangeRateId = exchangeRateEntry.Id,
                TripId = spot.TripId,
                SpotId = spot.Id,
                IsEstimated = true
            };

            // Dodaj expense bez uczestników
            await _expenseService.AddAsync(expense);

            _logger.LogInformation("Expense created for spot {SpotId}", spot.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense for spot {SpotId}", spot.Id);
        }
    }

    private async Task UpdateExpenseForSpot(Spot spot, SpotCreateEditViewModel viewModel)
    {
        // Tutaj można dodać logikę aktualizacji istniejącego Expense
        // jeśli będzie potrzebna w przyszłości
        // Na razie tworzymy tylko nowe Expense, nie aktualizujemy istniejących
    }

    private async Task PopulateSelectLists(SpotCreateEditViewModel viewModel, int tripId)
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

    private async Task PopulateCurrencySelectList(SpotCreateEditViewModel viewModel, IReadOnlyList<ExchangeRate> usedRates)
    {
        var allCurrencyCodes = Enum.GetValues(typeof(CurrencyCode))
            .Cast<CurrencyCode>()
            .ToDictionary(code => code, code => code.GetDisplayName());

        var usedCurrencies = usedRates
            .Select(er => new CurrencySelectGroupItem
            {
                Key = er.CurrencyCodeKey,
                Name = er.Name,
                ExchangeRate = er.ExchangeRateValue,
                IsUsed = true
            })
            .OrderBy(c => c.Key.ToString())
            .ThenByDescending(c => c.ExchangeRate)
            .ToList();

        var allCurrencies = allCurrencyCodes
            .Select(pair => new CurrencySelectGroupItem
            {
                Key = pair.Key,
                Name = pair.Value,
                ExchangeRate = 1.0M,
                IsUsed = false
            })
            .OrderBy(c => c.Key.ToString())
            .ToList();
        
        viewModel.CurrenciesGroups = usedCurrencies
            .Concat(allCurrencies)
            .ToList();

        // Ustaw domyślne wartości jeśli nie ustawione
        if (!viewModel.ExpenseCurrencyCode.HasValue && viewModel.ExpenseValue.HasValue)
        {
            try
            {
                var tripCurrency = await _tripService.GetTripCurrencyAsync(viewModel.TripId);

                // Szukaj najpierw w użytych walutach
                var defaultCurrency = usedCurrencies.FirstOrDefault(c => c.Key == tripCurrency);

                // Jeśli nie znaleziono w użytych, szukaj we wszystkich
                if (defaultCurrency == null)
                {
                    defaultCurrency = allCurrencies.FirstOrDefault(c => c.Key == tripCurrency);
                }

                // Jeśli nadal nie znaleziono, użyj pierwszej z użytych lub PLN
                defaultCurrency ??= usedCurrencies.FirstOrDefault()
                                  ?? allCurrencies.FirstOrDefault(c => c.Key == CurrencyCode.PLN)
                                  ?? allCurrencies.FirstOrDefault();

                if (defaultCurrency != null)
                {
                    viewModel.ExpenseCurrencyCode = defaultCurrency.Key;
                    // Dla waluty podróży ustawiamy kurs na 1
                    viewModel.ExpenseExchangeRateValue = 1.0M;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get trip currency for trip {TripId}, using fallback", viewModel.TripId);

                // Fallback: użyj pierwszej dostępnej waluty
                var fallbackCurrency = usedCurrencies.FirstOrDefault()
                                     ?? allCurrencies.FirstOrDefault(c => c.Key == CurrencyCode.PLN)
                                     ?? allCurrencies.FirstOrDefault();

                if (fallbackCurrency != null)
                {
                    viewModel.ExpenseCurrencyCode = fallbackCurrency.Key;
                    viewModel.ExpenseExchangeRateValue = 1.0M;
                }
            }
        }
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
        itemsInDay = itemsInDay.Where(a => a.DayId == dayId && !(a is Accommodation)).ToList();

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
            .Where(a => a.DayId == dayId && !(a is Accommodation))
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