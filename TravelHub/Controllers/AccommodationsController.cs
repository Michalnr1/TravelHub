using System.Security.Cryptography.Xml;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Web.ViewModels.Accommodations;
using TravelHub.Web.ViewModels.Expenses;
using CategorySelectItem = TravelHub.Web.ViewModels.Accommodations.CategorySelectItem;

namespace TravelHub.Web.Controllers;

[Authorize]
public class AccommodationsController : Controller
{
    private readonly IAccommodationService _accommodationService;
    private readonly ICategoryService _categoryService;
    private readonly ITripService _tripService;
    private readonly ITripParticipantService _tripParticipantService;
    private readonly IDayService _dayService;
    private readonly ISpotService _spotService;
    private readonly IExchangeRateService _exchangeRateService;
    private readonly IReverseGeocodingService _reverseGeocodingService;
    private readonly IExpenseService _expenseService;
    private readonly UserManager<Person> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AccommodationsController> _logger;

    public AccommodationsController(
        IAccommodationService accommodationService,
        ICategoryService categoryService,
        ITripService tripService,
        ITripParticipantService tripParticipantService,
        IDayService dayService,
        ISpotService spotService,
        IExchangeRateService exchangeRateService,
        IReverseGeocodingService reverseGeocodingService,
        IExpenseService expenseService,
        UserManager<Person> userManager,
        IConfiguration configuration,
        ILogger<AccommodationsController> logger)
    {
        _accommodationService = accommodationService;
        _categoryService = categoryService;
        _tripService = tripService;
        _tripParticipantService = tripParticipantService;
        _dayService = dayService;
        _spotService = spotService;
        _exchangeRateService = exchangeRateService;
        _reverseGeocodingService = reverseGeocodingService;
        _expenseService = expenseService;
        _userManager = userManager;
        _configuration = configuration;
        _logger = logger;
    }

    // GET: Accommodations
    public async Task<IActionResult> Index()
    {
        var accommodations = await _accommodationService.GetAllAsync();
        var viewModel = accommodations.Select(a => new AccommodationViewModel
        {
            Id = a.Id,
            Name = a.Name,
            Description = a.Description,
            // Cost = a.Cost,
            CategoryName = a.Category?.Name,
            DayName = a.Day?.Name,
            CheckIn = a.CheckIn,
            CheckOut = a.CheckOut
        }).ToList();

        return View(viewModel);
    }

    // GET: Accommodations/Details/5
    public async Task<IActionResult> Details(int? id, string source = "", string? returnUrl = null)
    {
        if (id == null)
        {
            return NotFound();
        }

        var accommodation = await _accommodationService.GetByIdWithDetailsAsync(id.Value);
        if (accommodation == null)
        {
            return NotFound();
        }

        if (source != "public" && !await _tripParticipantService.UserHasAccessToTripAsync(accommodation.TripId, GetCurrentUserId()))
        {
            return Forbid();
        }

        var viewModel = new AccommodationDetailsViewModel
        {
            Id = accommodation.Id,
            Name = accommodation.Name,
            Description = accommodation.Description,
            Duration = accommodation.Duration,
            Order = accommodation.Order,
            CategoryName = accommodation.Category?.Name,
            DayName = accommodation.Day?.Name,
            Longitude = accommodation.Longitude,
            Latitude = accommodation.Latitude,
            // Cost = accommodation.Cost,
            CheckIn = accommodation.CheckIn,
            CheckOut = accommodation.CheckOut,
            CheckInTime = accommodation.CheckInTime,
            CheckInTimeString = ConvertDecimalToTimeString(accommodation.CheckInTime),
            CheckOutTime = accommodation.CheckOutTime,
            CheckOutTimeString = ConvertDecimalToTimeString(accommodation.CheckOutTime),
            TripId = accommodation.TripId,
            TripName = accommodation.Trip?.Name
        };
        if (returnUrl != null)
            returnUrl = source == "public" ? returnUrl + "?source=public" : returnUrl;
        ViewData["ReturnUrl"] = returnUrl ?? (source == "public" ? Url.Action("Details", "TripsSearch", new { id = accommodation.TripId }) : Url.Action("Details", "Trips", new { id = accommodation.TripId }));
        return View(viewModel);
    }

    // GET: Accommodations/Create
    public async Task<IActionResult> Create()
    {
        var viewModel = await CreateAccommodationCreateEditViewModel();
        return View(viewModel);
    }

    // POST: Accommodations/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AccommodationCreateEditViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            viewModel.CheckInTime = ConvertTimeStringToDecimal(viewModel.CheckInTimeString);
            viewModel.CheckOutTime = ConvertTimeStringToDecimal(viewModel.CheckOutTimeString);

            var accommodation = new Accommodation
            {
                Name = viewModel.Name,
                Description = viewModel.Description,
                Duration = viewModel.Duration,
                Order = viewModel.Order,
                CategoryId = viewModel.CategoryId,
                TripId = viewModel.TripId,
                DayId = viewModel.DayId,
                Longitude = viewModel.Longitude,
                Latitude = viewModel.Latitude,
                // Cost = viewModel.Cost,
                CheckIn = viewModel.CheckIn,
                CheckOut = viewModel.CheckOut,
                CheckInTime = viewModel.CheckInTime,
                CheckOutTime = viewModel.CheckOutTime
            };

            await _accommodationService.AddAsync(accommodation);
            return RedirectToAction(nameof(Index));
        }

        await PopulateSelectListsForTrip(viewModel, viewModel.TripId);
        return View(viewModel);
    }

    // GET: Accommodations/Edit/5
    public async Task<IActionResult> Edit(int? id, string? returnUrl = null)
    {
        if (id == null)
        {
            return NotFound();
        }

        var accommodation = await _accommodationService .GetByIdWithDetailsAsync(id.Value);
        if (accommodation == null)
        {
            return NotFound();
        }

        if (!await _tripParticipantService.UserHasAccessToTripAsync(accommodation.TripId, GetCurrentUserId()))
        {
            return Forbid();
        }

        var viewModel = await CreateAccommodationCreateEditViewModel(accommodation);
        viewModel.CheckInTimeString = ConvertDecimalToTimeString(accommodation.CheckInTime);
        viewModel.CheckOutTimeString = ConvertDecimalToTimeString(accommodation.CheckOutTime);

        await PopulateSelectListsForTrip(viewModel, accommodation.TripId);

        // Dodaj dane potrzebne dla widoku (jak w AddToTrip)
        var trip = await _tripService.GetByIdAsync(accommodation.TripId);
        if (trip != null)
        {
            ViewData["TripName"] = trip.Name;
            ViewData["MinDate"] = trip.StartDate.ToString("yyyy-MM-dd");
            ViewData["MaxDate"] = trip.EndDate.ToString("yyyy-MM-dd");
        }

        ViewData["ReturnUrl"] = returnUrl;
        ViewData["GoogleApiKey"] = _configuration["ApiKeys:GoogleApiKey"];

        return View(viewModel);
    }

    // POST: Accommodations/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AccommodationCreateEditViewModel viewModel, string? returnUrl = null)
    {
        if (id != viewModel.Id)
        {
            return NotFound();
        }

        if (!await _tripParticipantService.UserHasAccessToTripAsync(viewModel.TripId, GetCurrentUserId()))
        {
            return Forbid();
        }

        // Walidacja dat w zakresie podróży (jak w AddToTrip)
        var trip = await _tripService.GetByIdAsync(viewModel.TripId);
        if (trip != null)
        {
            if (viewModel.CheckIn < trip.StartDate || viewModel.CheckIn > trip.EndDate)
            {
                ModelState.AddModelError("CheckIn", $"Check-in date must be between {trip.StartDate:yyyy-MM-dd} and {trip.EndDate:yyyy-MM-dd}");
            }

            if (viewModel.CheckOut < trip.StartDate || viewModel.CheckOut > trip.EndDate)
            {
                ModelState.AddModelError("CheckOut", $"Check-out date must be between {trip.StartDate:yyyy-MM-dd} and {trip.EndDate:yyyy-MM-dd}");
            }

            if (viewModel.CheckOut <= viewModel.CheckIn)
            {
                ModelState.AddModelError("CheckOut", "Check-out date must be after check-in date");
            }

            // Sprawdź konflikt dat
            bool hasConflict = await _accommodationService.HasDateConflictAsync(viewModel.TripId, viewModel.CheckIn, viewModel.CheckOut, id);

            if (hasConflict)
            {
                var conflicts = await GetConflictingAccommodations(viewModel.TripId, viewModel.CheckIn, viewModel.CheckOut, id);
                var conflictNames = string.Join(", ", conflicts.Select(c => c.Name));

                ModelState.AddModelError("",
                    $"The accommodation dates conflict with existing accommodations: {conflictNames}. " +
                    $"Please choose different dates or edit/delete the conflicting accommodation(s) first.");
            }
        }

        if (ModelState.IsValid)
        {
            try
            {
                viewModel.CheckInTime = ConvertTimeStringToDecimal(viewModel.CheckInTimeString);
                viewModel.CheckOutTime = ConvertTimeStringToDecimal(viewModel.CheckOutTimeString);

                var existingAccommodation = await _accommodationService.GetByIdAsync(id);
                if (existingAccommodation == null)
                {
                    return NotFound();
                }

                // SPRÓBUJ ZNALEŹĆ DZIEŃ DLA ACCOMMODATION (jak w AddToTrip)
                var days = await TryFindDaysForAccommodation(viewModel.TripId, viewModel.CheckIn, viewModel.CheckOut);

                // Update properties
                existingAccommodation.Name = viewModel.Name;
                existingAccommodation.Description = viewModel.Description;
                existingAccommodation.Duration = 0; // Duration nie jest istotne dla zakwaterowania
                existingAccommodation.Order = 0; // Order nie jest edytowalny
                existingAccommodation.CategoryId = viewModel.CategoryId;
                existingAccommodation.Days = days?.ToList()!;
                existingAccommodation.Longitude = viewModel.Longitude;
                existingAccommodation.Latitude = viewModel.Latitude;
                existingAccommodation.CheckIn = viewModel.CheckIn;
                existingAccommodation.CheckOut = viewModel.CheckOut;
                existingAccommodation.CheckInTime = viewModel.CheckInTime;
                existingAccommodation.CheckOutTime = viewModel.CheckOutTime;

                await _accommodationService.UpdateAsync(existingAccommodation);

                (string? countryName, string? countryCode, string? city) = await _reverseGeocodingService.GetCountryAndCity(viewModel.Latitude, viewModel.Longitude);
                if (countryName != null && countryCode != null)
                {
                    await _spotService.AddCountry(existingAccommodation.Id, countryName, countryCode);
                }

                // Aktualizuj powiązany Expense (jak w AddToTrip)
                await UpdateExpenseForAccommodation(existingAccommodation, viewModel);

                await AddAccommodationToDays(existingAccommodation.Days, existingAccommodation.Id);

                TempData["SuccessMessage"] = "Accommodation updated successfully!" +
                    (days == null ? " It will be automatically assigned to a day when one is created for its date range." : "");

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Details", "Accommodations", new { id = id });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await AccommodationExists(viewModel.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating accommodation");
                ModelState.AddModelError("", "An error occurred while updating the accommodation.");
            }
        }

        // W przypadku błędu, ponownie wypełnij listy i dane widoku
        await PopulateSelectListsForTrip(viewModel, viewModel.TripId);

        var accommodationForViewData = await _accommodationService.GetByIdAsync(id);
        if (accommodationForViewData != null)
        {
            var tripForViewData = await _tripService.GetByIdAsync(accommodationForViewData.TripId);
            if (tripForViewData != null)
            {
                ViewData["TripName"] = tripForViewData.Name;
                ViewData["MinDate"] = tripForViewData.StartDate.ToString("yyyy-MM-dd");
                ViewData["MaxDate"] = tripForViewData.EndDate.ToString("yyyy-MM-dd");

                (double lat, double lng) = await GetMedianCoords(tripForViewData.Id);
                ViewData["Latitude"] = lat;
                ViewData["Longitude"] = lng;
            }
        }

        ViewData["ReturnUrl"] = Url.Action("Details", "Accommodations", new { id = id });
        ViewData["GoogleApiKey"] = _configuration["ApiKeys:GoogleApiKey"];

        return View(viewModel);
    }

    // GET: Accommodations/Delete/5
    public async Task<IActionResult> Delete(int? id, string? returnUrl = null)
    {
        if (id == null)
        {
            return NotFound();
        }

        var accommodation = await _accommodationService.GetByIdAsync(id.Value);
        if (accommodation == null)
        {
            return NotFound();
        }

        if (!await _tripParticipantService.UserHasAccessToTripAsync(accommodation.TripId, GetCurrentUserId()))
        {
            return Forbid();
        }

        var viewModel = new AccommodationDetailsViewModel
        {
            Id = accommodation.Id,
            Name = accommodation.Name,
            Description = accommodation.Description,
            // Cost = accommodation.Cost,
            CheckIn = accommodation.CheckIn,
            CheckOut = accommodation.CheckOut
        };

        ViewData["ReturnUrl"] = returnUrl;
        return View(viewModel);
    }

    // POST: Accommodations/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, string? returnUrl = null)
    {
        var accommodation = await _accommodationService.GetByIdAsync(id);
        if (!await _tripParticipantService.UserHasAccessToTripAsync(accommodation.TripId, GetCurrentUserId()))
        {
            return Forbid();
        }
        await _accommodationService.DeleteAsync(id);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Details", "Trips", new { id = accommodation.TripId });
    }

    // GET: Accommodations/AddToTrip/5
    public async Task<IActionResult> AddToTrip(int tripId, int? dayId = null)
    {
        var trip = await _tripService.GetByIdAsync(tripId);
        if (trip == null)
        {
            return NotFound();
        }

        if (!await _tripParticipantService.UserHasAccessToTripAsync(tripId, GetCurrentUserId()))
        {
            return Forbid();
        }

        DateTime? dayDate = null;
        if (dayId != null)
        {
            var day = await _dayService.GetDayByIdAsync(dayId.Value);
            if (day == null)
            {
                return NotFound();
            }
            dayDate = day.Date;
        }

        var viewModel = new AccommodationCreateEditViewModel
        {
            TripId = tripId,
            Order = 0, // Order nie jest edytowalny przez użytkownika
            Duration = 0, // Duration nie jest istotne dla zakwaterowania
            CheckIn = dayDate ?? trip.StartDate,
            CheckOut = (dayDate ?? trip.StartDate).AddDays(1),
            CheckInTime = 14.0m,
            CheckOutTime = 10.0m,
            CheckInTimeString = "14:00",
            CheckOutTimeString = "10:00",
            TripCurrency = trip.CurrencyCode
        };

        await PopulateSelectListsForTrip(viewModel, tripId);

        ViewData["TripName"] = trip.Name;
        if (dayId != null)
        {
            ViewData["ReturnUrl"] = Url.Action("Details", "Days", new { id = dayId });
        }
        else
        {
            ViewData["ReturnUrl"] = Url.Action("Details", "Trips", new { id = tripId });
        }
        ViewData["GoogleApiKey"] = _configuration["ApiKeys:GoogleApiKey"];
        ViewData["MinDate"] = trip.StartDate.ToString("yyyy-MM-dd");
        ViewData["MaxDate"] = trip.EndDate.ToString("yyyy-MM-dd");

        (double lat, double lng) = await GetMedianCoords(tripId);

        ViewData["Latitude"] = lat;
        ViewData["Longitude"] = lng;

        return View("AddToTrip", viewModel);
    }

    // POST: Accommodations/AddToTrip/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToTrip(int tripId, AccommodationCreateEditViewModel viewModel)
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

        // Walidacja dat w zakresie podróży
        if (viewModel.CheckIn < trip.StartDate || viewModel.CheckIn > trip.EndDate)
        {
            ModelState.AddModelError("CheckIn", $"Check-in date must be between {trip.StartDate:yyyy-MM-dd} and {trip.EndDate:yyyy-MM-dd}");
        }

        if (viewModel.CheckOut < trip.StartDate || viewModel.CheckOut > trip.EndDate)
        {
            ModelState.AddModelError("CheckOut", $"Check-out date must be between {trip.StartDate:yyyy-MM-dd} and {trip.EndDate:yyyy-MM-dd}");
        }

        if (viewModel.CheckOut <= viewModel.CheckIn)
        {
            ModelState.AddModelError("CheckOut", "Check-out date must be after check-in date");
        }

        // Sprawdź konflikt dat
        bool hasConflict = await _accommodationService.HasDateConflictAsync(viewModel.TripId, viewModel.CheckIn, viewModel.CheckOut);

        if (hasConflict)
        {
            var conflicts = await GetConflictingAccommodations(viewModel.TripId, viewModel.CheckIn, viewModel.CheckOut);
            var conflictNames = string.Join(", ", conflicts.Select(c => c.Name));

            ModelState.AddModelError("",
                $"The accommodation dates conflict with existing accommodations: {conflictNames}. " +
                $"Please choose different dates or edit/delete the conflicting accommodation(s) first.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                viewModel.CheckInTime = ConvertTimeStringToDecimal(viewModel.CheckInTimeString);
                viewModel.CheckOutTime = ConvertTimeStringToDecimal(viewModel.CheckOutTimeString);

                // SPRÓBUJ ZNALEŹĆ DZIEŃ DLA ACCOMMODATION (tylko jeśli istnieje)
                var days = await TryFindDaysForAccommodation(tripId, viewModel.CheckIn, viewModel.CheckOut);

                var accommodation = new Accommodation
                {
                    Name = viewModel.Name,
                    Description = viewModel.Description,
                    Duration = 0, // Duration nie jest istotne dla zakwaterowania
                    Order = 0, // Order nie jest edytowalny
                    CategoryId = viewModel.CategoryId,
                    TripId = viewModel.TripId,
                    Days = days?.ToList()!,
                    Longitude = viewModel.Longitude,
                    Latitude = viewModel.Latitude,
                    // Cost = viewModel.Cost,
                    CheckIn = viewModel.CheckIn,
                    CheckOut = viewModel.CheckOut,
                    CheckInTime = viewModel.CheckInTime,
                    CheckOutTime = viewModel.CheckOutTime
                };

                // Dodaj accommodation
                var createdAccommodation = await _accommodationService.AddAsync(accommodation);

                (string? countryName, string? countryCode, string? city) = await _reverseGeocodingService.GetCountryAndCity(viewModel.Latitude, viewModel.Longitude);
                if (countryName != null && countryCode != null)
                {
                    await _spotService.AddCountry(accommodation.Id, countryName, countryCode);
                }

                // Jeśli podano koszt, utwórz powiązany Expense (opcjonalnie)
                if (viewModel.ExpenseValue.HasValue && viewModel.ExpenseValue > 0)
                {
                    await CreateExpenseForAccommodation(createdAccommodation, viewModel);
                }

                await AddAccommodationToDays(createdAccommodation.Days, createdAccommodation.Id);

                TempData["SuccessMessage"] = "Accommodation added successfully!" +
                    (days == null ? " It will be automatically assigned to a day when one is created for its date range." : "");

                return RedirectToAction("Details", "Trips", new { id = tripId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding accommodation to trip");
                ModelState.AddModelError("", "An error occurred while adding the accommodation.");
            }
        }

        await PopulateSelectListsForTrip(viewModel, tripId);
        ViewData["TripName"] = trip.Name;
        ViewData["ReturnUrl"] = Url.Action("Details", "Trips", new { id = tripId });
        ViewData["GoogleApiKey"] = _configuration["ApiKeys:GoogleApiKey"];
        ViewData["MinDate"] = trip.StartDate.ToString("yyyy-MM-dd");
        ViewData["MaxDate"] = trip.EndDate.ToString("yyyy-MM-dd");

        return View("AddToTrip", viewModel);
    }

    // Metoda pomocnicza do znalezienia dnia dla accommodation (tylko istniejące dni)
    private async Task<IEnumerable<Day>?> TryFindDaysForAccommodation(int tripId, DateTime checkIn, DateTime checkOut)
    {
        var days = await _dayService.GetDaysByTripIdAsync(tripId);

        var allMatchingDays = days.Where(d => d.Date.Date >= checkIn.Date && d.Date.Date < checkOut.Date);

        return allMatchingDays;
    }

    private async Task AddAccommodationToDays(IEnumerable<Day> days, int accommodationId)
    {
        foreach (Day day in days)
        {
            await _dayService.AddAccommodationToDay(day.Id, accommodationId);
        }
    }

    private async Task CreateExpenseForAccommodation(Accommodation accommodation, AccommodationCreateEditViewModel viewModel)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            _logger.LogWarning("Cannot create expense for accommodation: User not found");
            return;
        }

        try
        {
            // Pobierz lub utwórz exchange rate
            var exchangeRateEntry = await _exchangeRateService
                .GetOrCreateExchangeRateAsync(
                    accommodation.TripId,
                    viewModel.ExpenseCurrencyCode ?? CurrencyCode.PLN,
                    viewModel.ExpenseExchangeRateValue ?? 1.0M);

            // Utwórz Expense
            var expense = new Expense
            {
                Name = $"{accommodation.Name} (Expense)",
                EstimatedValue = viewModel.ExpenseValue!.Value,
                PaidById = currentUser.Id,
                CategoryId = accommodation.CategoryId,
                ExchangeRateId = exchangeRateEntry.Id,
                TripId = accommodation.TripId,
                SpotId = accommodation.Id,
                IsEstimated = true,
                Multiplier = (accommodation.CheckOut - accommodation.CheckIn).Days
            };

            // Dodaj expense bez uczestników
            await _expenseService.AddAsync(expense);

            _logger.LogInformation("Expense created for accommodation {AccommodationId}", accommodation.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense for accommodation {AccommodationId}", accommodation.Id);
        }
    }

    private async Task UpdateExpenseForAccommodation(Accommodation accommodation, AccommodationCreateEditViewModel viewModel)
    {
        try
        {
            var existingExpense = await _expenseService.GetExpenseByAccommodationIdAsync(accommodation.Id);

            if (viewModel.ExpenseValue.HasValue && viewModel.ExpenseValue > 0)
            {
                // Aktualizuj istniejący expense lub utwórz nowy
                if (existingExpense != null)
                {
                    // Pobierz lub utwórz nowy ExchangeRate
                    var exchangeRateEntry = await _exchangeRateService
                        .GetOrCreateExchangeRateAsync(
                            accommodation.TripId,
                            viewModel.ExpenseCurrencyCode ?? CurrencyCode.PLN,
                            viewModel.ExpenseExchangeRateValue ?? 1.0m);

                    existingExpense.EstimatedValue = viewModel.ExpenseValue.Value;
                    existingExpense.ExchangeRateId = exchangeRateEntry.Id;
                    existingExpense.ExchangeRate = exchangeRateEntry;
                    existingExpense.Multiplier = (accommodation.CheckOut - accommodation.CheckIn).Days;

                    await _expenseService.UpdateAsync(existingExpense);
                    _logger.LogInformation("Expense updated for accommodation {AccommodationId}", accommodation.Id);
                }
                else
                {
                    // Utwórz nowy expense
                    await CreateExpenseForAccommodation(accommodation, viewModel);
                }
            }
            else
            {
                // Jeśli koszt jest null lub 0, usuń istniejący expense (jeśli istnieje)
                if (existingExpense != null)
                {
                    await _expenseService.DeleteAsync(existingExpense.Id);
                    _logger.LogInformation("Expense deleted for accommodation {AccommodationId}", accommodation.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating expense for accommodation {AccommodationId}", accommodation.Id);
            // Nie rzucaj wyjątku dalej - błąd expense nie powinien blokować aktualizacji zakwaterowania
        }
    }

    private async Task PopulateSelectListsForTrip(AccommodationCreateEditViewModel viewModel, int tripId)
    {
        // Categories
        var categories = await _categoryService.GetAllCategoriesByTripAsync(viewModel.TripId);
        viewModel.Categories = categories.Select(c => new CategorySelectItem
        {
            Id = c.Id,
            Name = c.Name
        }).ToList();

        // Days - only for this trip (do wyświetlania informacji)
        var days = await _dayService.GetAllAsync();
        viewModel.Days = days.Where(d => d.TripId == tripId && d.Number.HasValue)
            .Select(d => new DaySelectItem
            {
                Id = d.Id,
                DisplayName = d.Name!
            }).ToList();

        // Currencies dla Expense
        var usedRates = await _exchangeRateService.GetTripExchangeRatesAsync(tripId);
        await PopulateCurrencySelectList(viewModel, usedRates);
    }

    private async Task PopulateCurrencySelectList(AccommodationCreateEditViewModel viewModel, IReadOnlyList<ExchangeRate> usedRates)
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

        // Ustaw domyślne wartości
        if (!viewModel.ExpenseCurrencyCode.HasValue)
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

    private async Task<int?> FindDayForDate(int tripId, DateTime date)
    {
        var days = await _dayService.GetAllAsync();
        var day = days.FirstOrDefault(d => d.TripId == tripId && d.Date.Date == date.Date);
        return day?.Id;
    }

    private async Task<(double lat, double lng)> GetMedianCoords(int tripId)
    {
        // Pobierz istniejące zakwaterowania i miejsca, aby ustalić medianę koordynatów
        var accommodations = await _accommodationService.GetAccommodationByTripAsync(tripId);
        var spots = await _spotService.GetSpotsByTripAsync(tripId); // Zakładając, że masz taki serwis

        var allCoords = accommodations.Select(a => (a.Latitude, a.Longitude))
                            .Concat(spots.Select(s => (s.Latitude, s.Longitude)))
                            .ToList();

        if (allCoords.Any())
        {
            var medianLat = allCoords.Select(c => c.Latitude).Average();
            var medianLng = allCoords.Select(c => c.Longitude).Average();
            return (medianLat, medianLng);
        }

        // Domyślne koordynaty (Warszawa)
        return (52.2297, 21.0122);
    }

    private async Task<bool> AccommodationExists(int id)
    {
        var accommodation = await _accommodationService.GetByIdAsync(id);
        return accommodation != null;
    }

    private async Task<AccommodationCreateEditViewModel> CreateAccommodationCreateEditViewModel(Accommodation? accommodation = null)
    {
        var viewModel = new AccommodationCreateEditViewModel();

        if (accommodation != null)
        {
            viewModel.Id = accommodation.Id;
            viewModel.Name = accommodation.Name;
            viewModel.Description = accommodation.Description;
            viewModel.Duration = accommodation.Duration;
            viewModel.Order = accommodation.Order;
            viewModel.CategoryId = accommodation.CategoryId;
            viewModel.TripId = accommodation.TripId;
            viewModel.DayId = accommodation.DayId;
            viewModel.Longitude = accommodation.Longitude;
            viewModel.Latitude = accommodation.Latitude;
            // viewModel.Cost = accommodation.Cost;
            viewModel.CheckIn = accommodation.CheckIn;
            viewModel.CheckOut = accommodation.CheckOut;
            viewModel.CheckInTime = accommodation.CheckInTime;
            viewModel.CheckOutTime = accommodation.CheckOutTime;
            viewModel.TripCurrency = await _tripService.GetTripCurrencyAsync(accommodation.TripId);

            if (accommodation.Expense != null)
            {
                viewModel.ExpenseValue = accommodation.Expense.EstimatedValue;
                viewModel.ExpenseCurrencyCode = accommodation.Expense.ExchangeRate!.CurrencyCodeKey;
                viewModel.ExpenseExchangeRateValue = accommodation.Expense.ExchangeRate!.ExchangeRateValue;
            }
        }

        await PopulateSelectListsForTrip(viewModel, viewModel.TripId);
        return viewModel;
    }

    private async Task PopulateSelectLists(AccommodationCreateEditViewModel viewModel)
    {
        // Categories
        var categories = await _categoryService.GetAllCategoriesByTripAsync(viewModel.TripId);
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

        // Days - filter by trip if TripId is set
        var days = await _dayService.GetAllAsync();
        var filteredDays = days.AsQueryable();

        if (viewModel.TripId > 0)
        {
            filteredDays = filteredDays.Where(d => d.TripId == viewModel.TripId);
        }

        viewModel.Days = filteredDays
            .Select(d => new DaySelectItem
            {
                Id = d.Id,
                DisplayName = d.Name!
            })
            .ToList();
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
    private string ConvertDecimalToTimeString(decimal time)
    {
        int hours = (int)time;
        int minutes = (int)((time - hours) * 60);
        return $"{hours:D2}:{minutes:D2}";
    }

    private string GetCurrentUserId()
    {
        return _userManager.GetUserId(User) ?? throw new UnauthorizedAccessException("User is not authenticated");
    }

    private async Task<List<Accommodation>> GetConflictingAccommodations(int tripId, DateTime checkIn, DateTime checkOut, int? excludeAccommodationId = null)
    {
        var accommodations = await _accommodationService.GetAccommodationByTripAsync(tripId);

        var conflictingAccommodations = new List<Accommodation>();

        foreach (var accommodation in accommodations)
        {
            if (excludeAccommodationId.HasValue && accommodation.Id == excludeAccommodationId.Value)
                continue;

            bool newCheckInInExistingRange = checkIn >= accommodation.CheckIn && checkIn < accommodation.CheckOut;
            bool newCheckOutInExistingRange = checkOut > accommodation.CheckIn && checkOut <= accommodation.CheckOut;
            bool containsExisting = checkIn <= accommodation.CheckIn && checkOut >= accommodation.CheckOut;

            if (newCheckInInExistingRange || newCheckOutInExistingRange || containsExisting)
            {
                conflictingAccommodations.Add(accommodation);
            }
        }

        return conflictingAccommodations;
    }
}