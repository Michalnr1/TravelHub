using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelHub.Domain.DTOs;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Infrastructure.Services;
using TravelHub.Web.ViewModels.Accommodations;
using TravelHub.Web.ViewModels.Activities;
using TravelHub.Web.ViewModels.Transports;
using TravelHub.Web.ViewModels.Trips;
using TravelHub.Web.ViewModels.TripsSearch;

[Authorize]
public class TripsSearchController : Controller
{
    private readonly ITripService _tripService;
    private readonly IAccommodationService _accommodationService;
    private readonly ITransportService _transportService;
    private readonly ISpotService _spotService;
    private readonly IActivityService _activityService;
    private readonly IExpenseService _expenseService;

    public TripsSearchController(ITripService tripService,
        IAccommodationService accommodationService,
        ITransportService transportService,
        ISpotService spotService,
        IActivityService activityService,
        IExpenseService expenseService
        )
    {
        _tripService = tripService;
        _accommodationService = accommodationService;
        _transportService = transportService;
        _spotService = spotService;
        _activityService = activityService;
        _expenseService = expenseService;
    }

    [HttpGet]
    public async Task<IActionResult> Search()
    {
        var availableCountries = await _tripService.GetAvailableCountriesForPublicTripsAsync();

        var emptyCriteria = new PublicTripSearchCriteriaDto();
        var publicTripDtos = await _tripService.SearchPublicTripsAsync(emptyCriteria);

        var tripViewModels = publicTripDtos.Select(dto => new PublicTripViewModel
        {
            Id = dto.Id,
            Name = dto.Name,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            OwnerName = dto.OwnerName,
            Countries = dto.Countries,
            SpotsCount = dto.SpotsCount,
            ParticipantsCount = dto.ParticipantsCount
        }).ToList();

        var viewModel = new PublicTripSearchViewModel
        {
            AvailableCountries = availableCountries.ToList(),
            Trips = tripViewModels
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Search(PublicTripSearchViewModel viewModel)
    {
        var criteria = new PublicTripSearchCriteriaDto
        {
            SearchTerm = viewModel.SearchTerm,
            CountryCode = viewModel.SelectedCountryCode,
            MinDays = viewModel.MinDays,
            MaxDays = viewModel.MaxDays
        };

        var publicTripDtos = await _tripService.SearchPublicTripsAsync(criteria);
        var availableCountries = await _tripService.GetAvailableCountriesForPublicTripsAsync();

        var tripViewModels = publicTripDtos.Select(dto => new PublicTripViewModel
        {
            Id = dto.Id,
            Name = dto.Name,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            OwnerName = dto.OwnerName,
            Countries = dto.Countries,
            SpotsCount = dto.SpotsCount,
            ParticipantsCount = dto.ParticipantsCount
        }).ToList();

        viewModel.Trips = tripViewModels;
        viewModel.AvailableCountries = availableCountries.ToList();

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var trip = await _tripService.GetTripWithDetailsAsync(id);
        if (trip == null)
        {
            return NotFound();
        }

        // Sprawdzamy czy wycieczka jest publiczna
        if (trip.IsPrivate)
        {
            return Forbid();
        }

        var activities = await _activityService.GetTripActivitiesWithDetailsAsync(id);
        var spots = await _spotService.GetTripSpotsWithDetailsAsync(id);
        var transports = await _transportService.GetTripTransportsWithDetailsAsync(id);
        var accommodations = await _accommodationService.GetAccommodationByTripAsync(id);
        var expenses = await _expenseService.GetByTripIdWithParticipantsAsync(id);
        var countries = await _spotService.GetCountriesByTripAsync(id);

        // Oblicz całkowite wydatki w walucie podróży
        var expensesSummary = await _expenseService.CalculateTripExpensesInTripCurrencyAsync(id, trip.CurrencyCode);

        // Mapowanie expenses - UKRYWAMY dane osobowe
        var expenseViewModels = expenses.Select(e =>
        {
            var calculation = expensesSummary.ExpenseCalculations
                .FirstOrDefault(calc => calc.ExpenseId == e.Id);

            return new PublicExpenseViewModel // Używamy specjalnego ViewModel dla publicznego widoku
            {
                Id = e.Id,
                Name = e.Name,
                Value = e.Value,
                EstimatedValue = e.EstimatedValue,
                // UKRYTE: PaidByName - nie pokazujemy kto zapłacił
                // UKRYTE: TransferredToName - nie pokazujemy transferów
                CategoryName = e.Category?.Name,
                CurrencyName = e.ExchangeRate?.Name ?? trip.CurrencyCode.GetDisplayName(),
                CurrencyCode = e.ExchangeRate?.CurrencyCodeKey ?? trip.CurrencyCode,
                ExchangeRateValue = e.ExchangeRate?.ExchangeRateValue ?? 1m,
                ConvertedValue = calculation?.ConvertedValue ?? e.Value,
                IsEstimated = e.IsEstimated,
                Multiplier = e.Multiplier,
                SpotId = e.SpotId,
                SpotName = e.Spot?.Name,
                TransportId = e.TransportId,
                TransportName = e.Transport?.Name,
                // Dodajemy flagę czy to transfer (do filtrowania)
                IsTransfer = !string.IsNullOrEmpty(e.TransferredToId)
            };
        }).ToList();

        // Filtrujemy wydatki
        var filteredExpenses = expenseViewModels
            .Where(e => !e.IsTransfer && e.IsEstimated)
            .ToList();

        var totalActualExpenses = expenseViewModels
            .Where(e => !e.IsEstimated && !e.IsTransfer)
            .Sum(e => e.ConvertedValue);

        var totalEstimatedExpenses = filteredExpenses
            .Sum(e => e.ConvertedValue * e.Multiplier);

        var viewModel = new PublicTripDetailViewModel
        {
            Id = trip.Id,
            Name = trip.Name,
            Status = trip.Status,
            StartDate = trip.StartDate,
            EndDate = trip.EndDate,
            IsPrivate = trip.IsPrivate,
            CurrencyCode = trip.CurrencyCode,
            OwnerName = !trip.Person!.IsPrivate ? $"{trip.Person?.FirstName} {trip.Person?.LastName}" : null,
            IsOwnerPublic = !trip.Person!.IsPrivate,

            // Collections
            Days = trip.Days?.Select(d => new DayViewModel
            {
                Id = d.Id,
                Number = d.Number,
                Name = d.Name,
                Date = d.Date,
                ActivitiesCount = d.Activities?.Count ?? 0
            }).ToList() ?? new List<DayViewModel>(),

            Activities = activities
                .Where(a => a is not Spot)
                .OrderBy(a => a.Day == null)
                .ThenBy(a => a.Day?.Number != null ? a.Day.Number : int.MaxValue)
                .ThenBy(a => a.Day?.Number == null ? a.Day?.Name : null)
                .ThenBy(a => a.Order)
                .Select(a => new ActivityViewModel
                {
                    Id = a.Id,
                    Name = a.Name,
                    Description = a.Description ?? string.Empty,
                    Duration = a.Duration,
                    DurationString = ConvertDecimalToTimeString(a.Duration),
                    Order = a.Order,
                    CategoryName = a.Category?.Name,
                    TripName = a.Trip?.Name ?? string.Empty,
                    DayName = a.Day?.Name
                }).ToList(),

            Spots = spots
                .Where(s => s is not Accommodation)
                .OrderBy(s => s.Day == null)
                .ThenBy(s => s.Day?.Number != null ? s.Day.Number : int.MaxValue)
                .ThenBy(s => s.Day?.Number == null ? s.Day?.Name : null)
                .ThenBy(s => s.Order)
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
                    DayName = s.Day?.Name,
                    Longitude = s.Longitude,
                    Latitude = s.Latitude,
                    PhotoCount = s.Photos?.Count ?? 0
                }).ToList(),

            Transports = transports.Select(t => new TransportViewModel
            {
                Id = t.Id,
                Name = t.Name,
                Type = t.Type,
                Duration = t.Duration,
                DurationString = ConvertDecimalToTimeString(t.Duration),
                TripName = t.Trip?.Name ?? string.Empty,
                FromSpotName = t.FromSpot?.Name ?? string.Empty,
                ToSpotName = t.ToSpot?.Name ?? string.Empty
            }).ToList(),

            Accommodations = accommodations.Select(a => new AccommodationViewModel
            {
                Id = a.Id,
                Name = a.Name,
                Description = a.Description ?? string.Empty,
                CategoryName = a.Category?.Name,
                DayName = a.Day?.Name,
                Days = a.Days,
                CheckIn = a.CheckIn,
                CheckOut = a.CheckOut,
                Latitude = a.Latitude,
                Longitude = a.Longitude
            }).ToList(),
            Countries = countries.Select(c => new CountryViewModel
            {
                Code = c.Code,
                Name = c.Name,
                SpotsCount = c.Spots?.Count ?? 0
            }).ToList(),

            Expenses = filteredExpenses, // Używamy przefiltrowanych wydatków
            TotalExpenses = totalActualExpenses, // Suma wydatków rzeczywistych
            EstimatedExpensesTotal = totalEstimatedExpenses, // Suma wydatków szacowanych

            // Statystyki uczestników (tylko liczba)
            ParticipantsCount = trip.Participants?.Count(p => (p.Status == TripParticipantStatus.Accepted || p.Status == TripParticipantStatus.Owner)) ?? 0
        };

        return View(viewModel);
    }

    private string ConvertDecimalToTimeString(decimal duration)
    {
        int hours = (int)duration;
        int minutes = (int)((duration - hours) * 60);
        return $"{hours:D2}:{minutes:D2}";
    }

    private IEnumerable<Spot> GetAllSpotsFromTrip(Trip trip)
    {
        var allActivities = trip.Activities.Union(trip.Days?.SelectMany(d => d.Activities) ?? Enumerable.Empty<Activity>());
        return allActivities.OfType<Spot>().GroupBy(s => s.Id).Select(g => g.First());
    }
}
