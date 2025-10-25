using Microsoft.Extensions.Logging;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;

namespace TravelHub.Infrastructure.Services;

public class TripService : GenericService<Trip>, ITripService
{
    private readonly ITripRepository _tripRepository;
    private readonly IDayRepository _dayRepository;
    private readonly IAccommodationService _accommodationService;
    private readonly ILogger<TripService> _logger;

    public TripService(ITripRepository tripRepository,
        IDayRepository dayRepository,
        IAccommodationService accommodationService,
        ILogger<TripService> logger)
        : base(tripRepository)
    {
        _tripRepository = tripRepository;
        _dayRepository = dayRepository;
        _accommodationService = accommodationService;
        _logger = logger;
    }

    public async Task<Trip?> GetTripWithDetailsAsync(int id)
    {
        return await _tripRepository.GetByIdWithDaysAsync(id);
    }

    public async Task<IEnumerable<Trip>> GetUserTripsAsync(string userId)
    {
        return await _tripRepository.GetByUserIdAsync(userId);
    }

    public async Task<Day> AddDayToTripAsync(int tripId, Day day)
    {
        var trip = await GetByIdAsync(tripId);
        if (trip == null)
        {
            throw new ArgumentException($"Trip with ID {tripId} not found");
        }

        // Walidacja logiki biznesowej
        if (day.Date < trip.StartDate || day.Date > trip.EndDate)
        {
            throw new ArgumentException("Day date must be within trip date range");
        }

        day.TripId = tripId;

        var existingDays = await _dayRepository.GetByTripIdAsync(tripId);
        if (existingDays.Any(d => d.Number.HasValue && d.Number == day.Number))
        {
            throw new ArgumentException($"Day with number {day.Number} already exists in this trip");
        }

        await _dayRepository.AddAsync(day);
        return day;
    }

    public async Task<IEnumerable<Day>> GetTripDaysAsync(int tripId)
    {
        return await _dayRepository.GetByTripIdAsync(tripId);
    }

    public async Task<bool> UserOwnsTripAsync(int tripId, string userId)
    {
        var trip = await GetByIdAsync(tripId);
        return trip?.PersonId == userId;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _tripRepository.ExistsAsync(id);
    }

    public async Task<(double medianLatitude, double medianLongitude)> GetMedianCoords(int id)
    {
        var trip = await GetTripWithDetailsAsync(id);

        if (trip == null)
        {
            throw new ArgumentException($"Trip with ID {id} not found");
        }

        var allSpots = new List<Spot>();

        // Spots directly in trip
        allSpots.AddRange(trip.Activities.OfType<Spot>());

        // Spots in each Day
        foreach (var day in trip.Days)
        {
            allSpots.AddRange(day.Activities.OfType<Spot>());
        }

        //Domyślnie jakiś default użytkownika?

        //if (!allSpots.Any())
        //    throw new InvalidOperationException("No spots found in this trip.");

        // Compute medians
        var medianLatitude = GetMedian(allSpots.Select(s => s.Latitude));
        var medianLongitude = GetMedian(allSpots.Select(s => s.Longitude));

        return (medianLatitude, medianLongitude);
    }

    public double GetMedian(IEnumerable<double> numbers)
    {
        if (numbers == null || numbers.Count() == 0) return 0;
        int count = numbers.Count();
        var orderedNumbers = numbers.OrderBy(p => p);
        double median = orderedNumbers.ElementAt(count / 2) + orderedNumbers.ElementAt((count - 1) / 2);
        median /= 2;
        return median;
    }

    public async Task<IEnumerable<Trip>> GetAllWithUserAsync()
    {
        return await _tripRepository.GetAllWithUserAsync();
    }

    public async Task<Day> CreateNextDayAsync(int tripId)
    {
        var trip = await GetByIdAsync(tripId);
        if (trip == null)
        {
            throw new ArgumentException($"Trip with ID {tripId} not found");
        }

        // 1. Get all existing days number
        var existingDayNumbers = (await _dayRepository.GetByTripIdAsync(tripId))
                                    .Where(d => d.Number.HasValue)
                                    .Select(d => d.Number!.Value)
                                    .OrderBy(n => n)
                                    .ToList();

        // 2. Find next day number
        int nextDayNumber = 1;

        for (int i = 0; i < existingDayNumbers.Count; i++)
        {
            if (existingDayNumbers[i] != i + 1)
            {
                nextDayNumber = i + 1;
                break;
            }

            nextDayNumber = existingDayNumbers.Count + 1;
        }

        // 3. Calculate date
        DateTime nextDayDate = trip.StartDate.Date.AddDays(nextDayNumber - 1);

        // 4. Date validation
        if (nextDayDate > trip.EndDate.Date)
        {
            throw new InvalidOperationException("Cannot add a new day. All dates within the trip range are already assigned to a day.");
        }

        // 5. Create new Day object
        var newDay = new Day
        {
            Number = nextDayNumber,
            Name = $"Day {nextDayNumber}",
            Date = nextDayDate,
            TripId = tripId
        };

        // 6. Add to repository
        await _dayRepository.AddAsync(newDay);

        // 7. AUTOMATYCZNIE PRZYPISZ ACCOMMODATION DO NOWEGO DNIA
        await AutoAssignAccommodationsToDay(newDay);

        return newDay;
    }

    // Metoda pomocnicza do automatycznego przypisywania accommodation do dnia
    private async Task AutoAssignAccommodationsToDay(Day day)
    {
        // Pobierz wszystkie accommodation z tej podróży bez przypisanego dnia
        var accommodationsWithoutDay = await _accommodationService.GetAccommodationByTripAsync(day.TripId);
        accommodationsWithoutDay = accommodationsWithoutDay
            .Where(a => a.DayId == null)
            .ToList();

        var assignedCount = 0;
        foreach (var accommodation in accommodationsWithoutDay)
        {
            // Sprawdź czy data dnia mieści się w zakresie check-in do check-out accommodation
            // (uwzględniamy dzień check-in, ale nie dzień check-out)
            if (day.Date >= accommodation.CheckIn.Date && day.Date < accommodation.CheckOut.Date)
            {
                accommodation.DayId = day.Id;
                await _accommodationService.UpdateAsync(accommodation);
                assignedCount++;

                _logger.LogInformation("Automatically assigned accommodation {AccommodationId} to day {DayId}",
                    accommodation.Id, day.Id);
            }
        }

        if (assignedCount > 0)
        {
            _logger.LogInformation("Automatically assigned {Count} accommodations to newly created day {DayId}",
                assignedCount, day.Id);
        }
    }
}