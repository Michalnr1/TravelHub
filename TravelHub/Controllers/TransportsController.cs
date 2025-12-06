using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Infrastructure.Services;
using TravelHub.Web.ViewModels.Expenses;
using TravelHub.Web.ViewModels.Transports;

namespace TravelHub.Web.Controllers;

[Authorize]
public class TransportsController : Controller
{
    private readonly ITransportService _transportService;
    private readonly ITripService _tripService;
    private readonly ITripParticipantService _tripParticipantService;
    private readonly ISpotService _spotService;
    private readonly IExpenseService _expenseService;
    private readonly IExchangeRateService _exchangeRateService;
    private readonly ILogger<TransportsController> _logger;
    private readonly UserManager<Person> _userManager;

    public TransportsController(
        ITransportService transportService,
        ITripService tripService,
        ITripParticipantService tripParticipantService,
        ISpotService spotService,
        IExpenseService expenseService,
        IExchangeRateService exchangeRateService,
        ILogger<TransportsController> logger,
        UserManager<Person> userManager)
    {
        _transportService = transportService;
        _tripService = tripService;
        _tripParticipantService = tripParticipantService;
        _spotService = spotService;
        _expenseService = expenseService;
        _exchangeRateService = exchangeRateService;
        _logger = logger;
        _userManager = userManager;
    }

    // GET: Transports
    public async Task<IActionResult> Index()
    {
        var transports = await _transportService.GetAllWithDetailsAsync();
        var viewModel = transports.Select(t => new TransportViewModel
        {
            Id = t.Id,
            Name = t.Name,
            Type = t.Type,
            Duration = t.Duration,
            DurationString = ConvertDecimalToTimeString(t.Duration),
            // Cost = t.Cost,
            TripName = t.Trip?.Name!,
            FromSpotName = t.FromSpot?.Name!,
            ToSpotName = t.ToSpot?.Name!
        }).ToList();

        return View(viewModel);
    }

    // GET: Transports/Details/5
    public async Task<IActionResult> Details(int? id, string source = "", string? returnUrl = null)
    {
        if (id == null)
        {
            return NotFound();
        }

        var transport = await _transportService.GetByIdWithDetailsAsync(id.Value);
        if (transport == null)
        {
            return NotFound();
        }

        if (source != "public" && !await _tripParticipantService.UserHasAccessToTripAsync(transport.TripId, GetCurrentUserId()))
        {
            return Forbid();
        }

        var viewModel = new TransportDetailsViewModel
        {
            Id = transport.Id,
            Name = transport.Name,
            Type = transport.Type,
            Duration = transport.Duration,
            DurationString = ConvertDecimalToTimeString(transport.Duration),
            // Cost = transport.Cost,
            TripName = transport.Trip?.Name!,
            TripId = transport.TripId,
            FromSpotName = transport.FromSpot?.Name!,
            ToSpotName = transport.ToSpot?.Name!,
            FromSpotCoordinates = transport.FromSpot != null ?
                $"{transport.FromSpot.Latitude:F4}, {transport.FromSpot.Longitude:F4}" : "N/A",
            ToSpotCoordinates = transport.ToSpot != null ?
                $"{transport.ToSpot.Latitude:F4}, {transport.ToSpot.Longitude:F4}" : "N/A"
        };

        if (returnUrl != null)
            returnUrl = source == "public" ? returnUrl + "?source=public" : returnUrl;
        ViewData["ReturnUrl"] = returnUrl ?? (source == "public" ? Url.Action("Details", "TripsSearch", new { id = transport.TripId }) : Url.Action("Details", "Trips", new { id = transport.TripId }));

        return View(viewModel);
    }

    // GET: Transports/Create
    public async Task<IActionResult> Create()
    {
        var viewModel = await CreateTransportCreateEditViewModel();
        viewModel.DurationString = "01:00";
        return View(viewModel);
    }

    // POST: Transports/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TransportCreateEditViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            // Check if FromSpot and ToSpot are different
            if (viewModel.FromSpotId == viewModel.ToSpotId)
            {
                ModelState.AddModelError("", "From spot and To spot cannot be the same.");
                await PopulateSelectListsForTrip(viewModel, viewModel.TripId);
                return View(viewModel);
            }

            var transport = new Transport
            {
                Name = viewModel.Name,
                Type = viewModel.Type,
                Duration = viewModel.Duration,
                // Cost = viewModel.Cost,
                TripId = viewModel.TripId,
                FromSpotId = viewModel.FromSpotId,
                ToSpotId = viewModel.ToSpotId
            };

            var createdTransport = await _transportService.AddAsync(transport);

            // Jeśli podano koszt, utwórz powiązany Expense
            if (viewModel.ExpenseValue.HasValue && viewModel.ExpenseValue > 0)
            {
                await CreateExpenseForTransport(createdTransport, viewModel);
            }

            return RedirectToAction("Details", "Trips", new { id = viewModel.TripId });
        }

        await PopulateSelectListsForTrip(viewModel, viewModel.TripId);
        return View(viewModel);
    }

    // GET: Transports/Edit/5
    public async Task<IActionResult> Edit(int? id, string? returnUrl = null)
    {
        if (id == null)
        {
            return NotFound();
        }

        var transport = await _transportService.GetByIdWithDetailsAsync(id.Value);
        if (transport == null)
        {
            return NotFound();
        }

        if (!await _tripParticipantService.UserHasAccessToTripAsync(transport.TripId, GetCurrentUserId()))
        {
            return Forbid();
        }

        var viewModel = await CreateTransportCreateEditViewModel(transport);
        viewModel.DurationString = ConvertDecimalToTimeString(transport.Duration);

        ViewData["ReturnUrl"] = returnUrl ?? Url.Action("Details", "Trips", new { id = transport.TripId });

        return View(viewModel);
    }

    // POST: Transports/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TransportCreateEditViewModel viewModel, string? returnUrl = null)
    {
        if (id != viewModel.Id)
        {
            return NotFound();
        }

        if (!await _tripParticipantService.UserHasAccessToTripAsync(viewModel.TripId, GetCurrentUserId()))
        {
            return Forbid();
        }

        // Check if FromSpot and ToSpot are different
        if (viewModel.FromSpotId == viewModel.ToSpotId)
        {
            ModelState.AddModelError("", "From spot and To spot cannot be the same.");
            await PopulateSelectListsForTrip(viewModel, viewModel.TripId);
            return View(viewModel);
        }

        if (ModelState.IsValid)
        {
            try
            {
                viewModel.Duration = ConvertTimeStringToDecimal(viewModel.DurationString);

                var existingTransport = await _transportService.GetByIdAsync(id);
                if (existingTransport == null)
                {
                    return NotFound();
                }

                existingTransport.Name = viewModel.Name;
                existingTransport.Type = viewModel.Type;
                existingTransport.Duration = viewModel.Duration;
                // existingTransport.Cost = viewModel.Cost;
                existingTransport.TripId = viewModel.TripId;
                existingTransport.FromSpotId = viewModel.FromSpotId;
                existingTransport.ToSpotId = viewModel.ToSpotId;

                await _transportService.UpdateAsync(existingTransport);

                await UpdateExpenseForTransport(existingTransport, viewModel);

                TempData["SuccessMessage"] = "Transport updated successfully!";

                // Aktualizacja Expense jeśli istnieje i zmieniono dane
                await UpdateExpenseForTransport(existingTransport, viewModel);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await TransportExists(viewModel.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Details", "Trips", new { id = viewModel.TripId });
        }

        await PopulateSelectListsForTrip(viewModel, viewModel.TripId);
        return View(viewModel);
    }

    // GET: Transports/Delete/5
    public async Task<IActionResult> Delete(int? id, string? returnUrl = null)
    {
        if (id == null)
        {
            return NotFound();
        }

        var transport = await _transportService.GetByIdAsync(id.Value);
        if (transport == null)
        {
            return NotFound();
        }

        if (!await _tripParticipantService.UserHasAccessToTripAsync(transport.TripId, GetCurrentUserId()))
        {
            return Forbid();
        }

        var viewModel = new TransportDetailsViewModel
        {
            Id = transport.Id,
            Name = transport.Name,
            Type = transport.Type,
            Duration = transport.Duration,
            DurationString = ConvertDecimalToTimeString(transport.Duration),
            // Cost = transport.Cost,
            TripName = transport.Trip?.Name!,
            TripId = transport.TripId,
            FromSpotName = transport.FromSpot?.Name!,
            ToSpotName = transport.ToSpot?.Name!
        };

        ViewData["ReturnUrl"] = returnUrl;
        return View(viewModel);
    }

    // POST: Transports/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, string? returnUrl = null)
    {
        var transport = await _transportService.GetByIdAsync(id);
        if (!await _tripParticipantService.UserHasAccessToTripAsync(transport.TripId, GetCurrentUserId()))
        {
            return Forbid();
        }
        await _transportService.DeleteAsync(id);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Details", "Trips", new { id = transport.TripId });
    }

    // GET: Transports/AddToTrip/5
    public async Task<IActionResult> AddToTrip(int tripId)
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

        var viewModel = new TransportCreateEditViewModel
        {
            TripId = tripId,
            TripCurrency = trip.CurrencyCode
        };

        await PopulateSelectListsForTrip(viewModel, tripId);

        ViewData["TripName"] = trip.Name;
        ViewData["ReturnUrl"] = Url.Action("Details", "Trips", new { id = tripId });

        return View("AddToTrip", viewModel);
    }

    // POST: Transports/AddToTrip/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToTrip(int tripId, TransportCreateEditViewModel viewModel)
    {
        if (tripId != viewModel.TripId)
        {
            return NotFound();
        }

        if (!await _tripParticipantService.UserHasAccessToTripAsync(tripId, GetCurrentUserId()))
        {
            return Forbid();
        }

        if (ModelState.IsValid)
        {
            // Check if FromSpot and ToSpot are different
            if (viewModel.FromSpotId == viewModel.ToSpotId)
            {
                ModelState.AddModelError("", "From spot and To spot cannot be the same.");
                await PopulateSelectListsForTrip(viewModel, tripId);
                return View("AddToTrip", viewModel);
            }

            try
            {
                viewModel.Duration = ConvertTimeStringToDecimal(viewModel.DurationString);

                var transport = new Transport
                {
                    Name = viewModel.Name,
                    Type = viewModel.Type,
                    Duration = viewModel.Duration,
                    // Cost = viewModel.Cost,
                    TripId = viewModel.TripId,
                    FromSpotId = viewModel.FromSpotId,
                    ToSpotId = viewModel.ToSpotId
                };

                var createdTransport = await _transportService.AddAsync(transport);

                // Jeśli podano koszt, utwórz powiązany Expense (opcjonalnie)
                if (viewModel.ExpenseValue.HasValue && viewModel.ExpenseValue > 0)
                {
                    await CreateExpenseForTransport(createdTransport, viewModel);
                }

                TempData["SuccessMessage"] = "Transport added successfully!";
                return RedirectToAction("Details", "Trips", new { id = tripId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding transport to trip");
                ModelState.AddModelError("", "An error occurred while adding the transport.");
            }
        }

        await PopulateSelectListsForTrip(viewModel, tripId);
        return View("AddToTrip", viewModel);
    }

    private string GetCurrentUserId()
    {
        return _userManager.GetUserId(User) ?? throw new UnauthorizedAccessException("User is not authenticated");
    }

    private async Task CreateExpenseForTransport(Transport transport, TransportCreateEditViewModel viewModel)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            _logger.LogWarning("Cannot create expense for transport: User not found");
            return;
        }

        try
        {
            // Pobierz lub utwórz exchange rate
            var exchangeRateEntry = await _exchangeRateService
                .GetOrCreateExchangeRateAsync(
                    transport.TripId,
                    viewModel.ExpenseCurrencyCode ?? CurrencyCode.PLN,
                    viewModel.ExpenseExchangeRateValue ?? 1.0M);

            // Utwórz Expense
            var expense = new Expense
            {
                Name = $"{transport.Name} (Transport)",
                EstimatedValue = viewModel.ExpenseValue!.Value,
                PaidById = currentUser.Id,
                CategoryId = null,
                ExchangeRateId = exchangeRateEntry.Id,
                TripId = transport.TripId,
                TransportId = transport.Id,
                IsEstimated = true
            };

            // Dodaj expense bez uczestników
            await _expenseService.AddAsync(expense);

            _logger.LogInformation("Expense created for transport {TransportId}", transport.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense for transport {TransportId}", transport.Id);
        }
    }

    private async Task UpdateExpenseForTransport(Transport transport, TransportCreateEditViewModel viewModel)
    {
        try
        {
            var existingExpense = await _expenseService.GetExpenseForTransportAsync(transport.Id);

            if (viewModel.ExpenseValue.HasValue && viewModel.ExpenseValue > 0)
            {
                // Aktualizuj istniejący expense lub utwórz nowy
                if (existingExpense != null)
                {
                    // Pobierz lub utwórz nowy ExchangeRate
                    var exchangeRateEntry = await _exchangeRateService
                        .GetOrCreateExchangeRateAsync(
                            transport.TripId,
                            viewModel.ExpenseCurrencyCode ?? CurrencyCode.PLN,
                            viewModel.ExpenseExchangeRateValue ?? 1.0m);

                    existingExpense.EstimatedValue = viewModel.ExpenseValue.Value;
                    existingExpense.ExchangeRateId = exchangeRateEntry.Id;
                    existingExpense.ExchangeRate = exchangeRateEntry;

                    await _expenseService.UpdateAsync(existingExpense);
                    _logger.LogInformation("Expense updated for transport {TransportId}", transport.Id);
                }
                else
                {
                    await CreateExpenseForTransport(transport, viewModel);
                }
            }
            else
            {
                // Usuń expense jeśli wartość jest pusta lub 0
                if (existingExpense != null)
                {
                    await _expenseService.DeleteAsync(existingExpense.Id);
                    _logger.LogInformation("Expense deleted for transport {TransportId}", transport.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating expense for transport {TransportId}", transport.Id);
        }
    }

    private async Task PopulateSelectListsForTrip(TransportCreateEditViewModel viewModel, int tripId)
    {
        // Spots - only for this trip
        var spots = await _spotService.GetSpotsByTripAsync(tripId);
        viewModel.Spots = spots.Select(s => new SpotSelectItem
        {
            Id = s.Id,
            Name = s.Name,
            TripId = s.TripId,
            Coordinates = $"{s.Latitude:F4}, {s.Longitude:F4}"
        }).ToList();

        // Transportation types
        viewModel.TransportationTypes = Enum.GetValues(typeof(TransportationType))
            .Cast<TransportationType>()
            .Select(t => new TransportationTypeSelectItem
            {
                Value = t,
                Name = t.ToString()
            }).ToList();

        // Currencies dla Expense
        var usedRates = await _exchangeRateService.GetTripExchangeRatesAsync(tripId);
        await PopulateCurrencySelectList(viewModel, usedRates);
    }

    private async Task PopulateCurrencySelectList(TransportCreateEditViewModel viewModel, IReadOnlyList<ExchangeRate> usedRates)
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

        // Ustaw domyślną walutę
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

    private async Task<bool> TransportExists(int id)
    {
        var transport = await _transportService.GetByIdAsync(id);
        return transport != null;
    }

    private async Task<TransportCreateEditViewModel> CreateTransportCreateEditViewModel(Transport? transport = null)
    {
        var viewModel = new TransportCreateEditViewModel();

        if (transport != null)
        {
            viewModel.Id = transport.Id;
            viewModel.Name = transport.Name;
            viewModel.Type = transport.Type;
            viewModel.Duration = transport.Duration;
            viewModel.TripId = transport.TripId;
            viewModel.FromSpotId = transport.FromSpotId;
            viewModel.ToSpotId = transport.ToSpotId;
            viewModel.TripCurrency = transport.Trip!.CurrencyCode;

            if (transport.Expense != null)
            {
                viewModel.ExpenseValue = transport.Expense.EstimatedValue;
                viewModel.ExpenseCurrencyCode = transport.Expense.ExchangeRate!.CurrencyCodeKey;
                viewModel.ExpenseExchangeRateValue = transport.Expense.ExchangeRate!.ExchangeRateValue;
            }

            await PopulateSelectListsForTrip(viewModel, transport.TripId);
        }
        return viewModel;
    }

    private async Task PopulateSelectLists(TransportCreateEditViewModel viewModel)
    {
        // Trips
        var trips = await _tripService.GetAllAsync();
        viewModel.Trips = trips.Select(t => new TripSelectItem
        {
            Id = t.Id,
            Name = t.Name
        }).ToList();

        // Spots - filter by selected trip if available
        var spots = await _spotService.GetAllAsync();
        if (viewModel.TripId > 0)
        {
            spots = spots.Where(s => s.TripId == viewModel.TripId).ToList();
        }
        viewModel.Spots = spots.Select(s => new SpotSelectItem
        {
            Id = s.Id,
            Name = s.Name,
            TripId = s.TripId,
            Coordinates = $"{s.Latitude:F4}, {s.Longitude:F4}"
        }).ToList();

        // Transportation types
        viewModel.TransportationTypes = Enum.GetValues(typeof(TransportationType))
            .Cast<TransportationType>()
            .Select(t => new TransportationTypeSelectItem
            {
                Value = t,
                Name = t.ToString()
            }).ToList();
    }

    // AJAX method to get spots for a trip
    public async Task<JsonResult> GetSpotsByTrip(int tripId)
    {
        var spots = await _spotService.GetSpotsByTripAsync(tripId);
        var spotList = spots.Select(s => new SpotSelectItem
        {
            Id = s.Id,
            Name = s.Name,
            TripId = s.TripId,
            Coordinates = $"{s.Latitude:F4}, {s.Longitude:F4}"
        }).ToList();

        return Json(spotList);
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
}