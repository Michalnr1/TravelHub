using Azure.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics.Metrics;
using TravelHub.Application.DTOs;
using TravelHub.Domain.DTOs;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Infrastructure.Repositories;
using TravelHub.Web.Utils;

namespace TravelHub.Infrastructure.Services;

public class TripService : GenericService<Trip>, ITripService
{
    private readonly ITripRepository _tripRepository;
    private readonly IDayRepository _dayRepository;
    private readonly IActivityRepository _activityRepository;
    private readonly ISpotRepository _spotRepository;
    private readonly IAccommodationService _accommodationService;
    private readonly ITransportRepository _transportRepository;
    private readonly IExpenseRepository _expenseRepository;
    private readonly IExchangeRateRepository _exchangeRateRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICurrencyConversionService _currencyConversionService;
    private readonly ITripParticipantRepository _tripParticipantRepository;
    private readonly IBlogRepository _blogRepository;
    private readonly ILogger<TripService> _logger;

    public TripService(ITripRepository tripRepository,
        IDayRepository dayRepository,
        IActivityRepository activityRepository,
        ISpotRepository spotRepository,
        IAccommodationService accommodationService,
        ITransportRepository transportRepository,
        IExpenseRepository expenseRepository,
        IExchangeRateRepository exchangeRateRepository,
        ICategoryRepository categoryRepository,
        ICurrencyConversionService currencyConversionService,
        ITripParticipantRepository tripParticipantRepository,
        IBlogRepository blogRepository,
        ILogger<TripService> logger)
        : base(tripRepository)
    {
        _tripRepository = tripRepository;
        _dayRepository = dayRepository;
        _activityRepository = activityRepository;
        _spotRepository = spotRepository;
        _accommodationService = accommodationService;
        _transportRepository = transportRepository;
        _expenseRepository = expenseRepository;
        _exchangeRateRepository = exchangeRateRepository;
        _categoryRepository = categoryRepository;
        _currencyConversionService = currencyConversionService;
        _tripParticipantRepository = tripParticipantRepository;
        _blogRepository = blogRepository;
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

        var accomms = await _accommodationService.GetAccommodationByTripAsync(id);

        allSpots.AddRange(accomms);

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

    public async Task<CurrencyCode> GetTripCurrencyAsync(int tripId)
    {
        var trip = await GetByIdAsync(tripId);
        if (trip == null)
        {
            throw new ArgumentException($"Trip with ID {tripId} not found");
        }

        return trip.CurrencyCode;
    }

    // Metoda pomocnicza do automatycznego przypisywania accommodation do dnia
    private async Task AutoAssignAccommodationsToDay(Day day)
    {
        // Pobierz wszystkie accommodation z tej podróży bez przypisanego dnia
        var accommodations = await _accommodationService.GetAccommodationByTripAsync(day.TripId);

        var assignedCount = 0;
        foreach (var accommodation in accommodations)
        {
            // Sprawdź czy data dnia mieści się w zakresie check-in do check-out accommodation
            // (uwzględniamy dzień check-in, ale nie dzień check-out)
            if (day.Date >= accommodation.CheckIn.Date && day.Date < accommodation.CheckOut.Date)
            {
                accommodation.DayId = day.Id;
                await _accommodationService.UpdateAsync(accommodation);
                day.AccommodationId = accommodation.Id;
                await _dayRepository.UpdateAsync(day);
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

    public async Task<IEnumerable<Person>> GetAllTripParticipantsAsync(int tripId)
    {
        return await _tripParticipantRepository.GetAllTripParticipantsAsync(tripId);
    }

    public async Task<Checklist> GetChecklistAsync(int tripId)
    {
        var trip = await _repository.GetByIdAsync(tripId);
        if (trip == null) throw new KeyNotFoundException();
        return trip.Checklist ?? new Checklist();
    }

    public async Task AddChecklistItemAsync(int tripId, string item)
    {
        var trip = await _repository.GetByIdAsync(tripId) ?? throw new KeyNotFoundException();
        trip.Checklist ??= new Checklist();
        trip.Checklist.AddItem(item, false);

        await _repository.UpdateAsync(trip);
    }

    public async Task ToggleChecklistItemAsync(int tripId, string item)
    {
        var trip = await _tripRepository.GetByIdWithParticipantsAsync(tripId) ?? throw new KeyNotFoundException(nameof(tripId));

        var current = trip.Checklist ?? new Checklist();

        // create deep copy so EF change tracker notices assignment
        var copy = new Checklist();
        foreach (var it in current.Items)
        {
            copy.Items.Add(new ChecklistItem
            {
                Title = it.Title,
                IsCompleted = it.IsCompleted,
                AssignedParticipantId = it.AssignedParticipantId,
                AssignedParticipantName = it.AssignedParticipantName
            });
        }

        // find item by title (case-sensitive - adjust if you want case-insensitive)
        var target = copy.Items.FirstOrDefault(x => x.Title == item);

        if (target != null)
        {
            // toggle existing item
            target.IsCompleted = !target.IsCompleted;
        }
        else
        {
            // add new item (completed)
            copy.Items.Add(new ChecklistItem
            {
                Title = item,
                IsCompleted = true
                // assigned remains null
            });
        }

        // assign new instance so EF will persist change
        trip.Checklist = copy;
        await _tripRepository.UpdateAsync(trip);
    }

    public async Task RemoveChecklistItemAsync(int tripId, string item)
    {
        var trip = await _repository.GetByIdAsync(tripId) ?? throw new KeyNotFoundException();
        if (trip.Checklist == null) return;
        trip.Checklist.RemoveItem(item);
        await _repository.UpdateAsync(trip);
    }

    public async Task ReplaceChecklistAsync(int tripId, Checklist newChecklist)
    {
        var trip = await _repository.GetByIdAsync(tripId) ?? throw new KeyNotFoundException();
        trip.Checklist = newChecklist ?? new Checklist();
        await _repository.UpdateAsync(trip);
    }

    public async Task RenameChecklistItemAsync(int tripId, string oldItem, string newItem)
    {
        if (string.IsNullOrWhiteSpace(newItem))
            throw new ArgumentException("New title must be provided.", nameof(newItem));

        var trip = await _tripRepository.GetByIdWithParticipantsAsync(tripId) ?? throw new KeyNotFoundException(nameof(tripId));

        var current = trip.Checklist ?? new Checklist();

        // deep copy
        var copy = new Checklist();
        foreach (var it in current.Items)
        {
            copy.Items.Add(new ChecklistItem
            {
                Title = it.Title,
                IsCompleted = it.IsCompleted,
                AssignedParticipantId = it.AssignedParticipantId,
                AssignedParticipantName = it.AssignedParticipantName
            });
        }

        // find existing by oldTitle
        var target = copy.Items.FirstOrDefault(x => x.Title == oldItem);
        if (target == null)
            throw new KeyNotFoundException("Item to rename not found.");

        // if newTitle equals oldTitle -> nothing to do
        if (string.Equals(oldItem, newItem, StringComparison.Ordinal))
        {
            return;
        }

        // check duplicate (case-sensitive). Use OrdinalIgnoreCase if you want case-insensitive.
        if (copy.Items.Any(x => x.Title == newItem))
            throw new InvalidOperationException("An item with the same title already exists.");

        // perform rename (preserve other fields)
        target.Title = newItem;

        // assign and persist
        trip.Checklist = copy;
        await _tripRepository.UpdateAsync(trip);
    }

    public async Task AssignParticipantToItemAsync(int tripId, string itemTitle, string? participantId)
    {
        var trip = await _tripRepository.GetByIdWithParticipantsAsync(tripId) ?? throw new KeyNotFoundException();
        var current = trip.Checklist ?? new Checklist();

        // deep copy
        var copy = new Checklist();
        foreach (var it in current.Items)
            copy.Items.Add(new ChecklistItem
            {
                Title = it.Title,
                IsCompleted = it.IsCompleted,
                AssignedParticipantId = it.AssignedParticipantId,
                AssignedParticipantName = it.AssignedParticipantName
            });

        var target = copy.Items.FirstOrDefault(i => i.Title == itemTitle);
        if (target == null) throw new KeyNotFoundException();

        // if participantId is null or empty -> unassign
        if (string.IsNullOrWhiteSpace(participantId))
        {
            target.AssignedParticipantId = null;
            target.AssignedParticipantName = null;
        }
        else
        {
            // optional: validate participant belongs to trip
            var participant = trip.Participants?.FirstOrDefault(p => (p.Id.ToString() ?? p.PersonId) == participantId);
            if (participant == null)
                throw new InvalidOperationException("Participant does not belong to this trip.");

            target.AssignedParticipantId = participantId;
            target.AssignedParticipantName = participant.Person != null
                ? $"{participant.Person.FirstName} {participant.Person.LastName}"
                : participant.PersonId ?? participantId;
        }

        trip.Checklist = copy;
        await _tripRepository.UpdateAsync(trip);
    }

    public async Task<Trip?> GetByIdWithParticipantsAsync(int tripId)
    {
        return await _tripRepository.GetByIdWithParticipantsAsync(tripId);
    }

    public async Task<Blog?> GetOrCreateBlogForTripAsync(int tripId, string userId)
    {
        var trip = await _tripRepository.GetByIdAsync(tripId);
        if (trip == null)
            return null;

        // Sprawdź czy użytkownik jest właścicielem tripa
        if (trip.PersonId != userId)
            return null;

        // Sprawdź czy blog już istnieje
        var existingBlog = await _blogRepository.GetByTripIdAsync(tripId);
        if (existingBlog != null)
            return existingBlog;

        // Utwórz nowy blog
        var blog = new Blog
        {
            Name = $"{trip.Name} - Blog",
            Description = $"Blog for trip: {trip.Name}",
            OwnerId = userId,
            TripId = tripId,
            Catalog = "travel"
        };

        return await _blogRepository.AddAsync(blog);
    }

    public async Task<bool> HasBlogAsync(int tripId)
    {
        var blog = await _blogRepository.GetByTripIdAsync(tripId);
        return blog != null;
    }

    public async Task<IEnumerable<PublicTripDto>> SearchPublicTripsAsync(PublicTripSearchCriteriaDto criteria)
    {
        var allPublicTrips = await _tripRepository.GetPublicTripsAsync();

        var filteredTrips = allPublicTrips.AsEnumerable();

        // Filtrowanie po frazie wyszukiwania (nazwa podróży lub nazwy miejsc)
        if (!string.IsNullOrEmpty(criteria.SearchTerm))
        {
            var searchTerm = criteria.SearchTerm.ToLower();
            filteredTrips = filteredTrips.Where(t =>
                t.Name.ToLower().Contains(searchTerm) ||
                GetAllSpotsFromTrip(t).Any(s => s.Name.ToLower().Contains(searchTerm))
            );
        }

        // Filtrowanie po kraju
        if (!string.IsNullOrEmpty(criteria.CountryCode))
        {
            filteredTrips = filteredTrips.Where(t =>
                GetAllSpotsFromTrip(t).Any(s => s.CountryCode == criteria.CountryCode)
            );
        }

        // Filtrowanie po długości podróży (w dniach)
        if (criteria.MinDays.HasValue || criteria.MaxDays.HasValue)
        {
            filteredTrips = filteredTrips.Where(t => {
                var duration = (t.EndDate - t.StartDate).Days + 1;

                if (criteria.MinDays.HasValue && criteria.MaxDays.HasValue)
                    return duration >= criteria.MinDays.Value && duration <= criteria.MaxDays.Value;
                else if (criteria.MinDays.HasValue)
                    return duration >= criteria.MinDays.Value;
                else if (criteria.MaxDays.HasValue)
                    return duration <= criteria.MaxDays.Value;
                else
                    return true;
            });
        }

        return filteredTrips
            .OrderBy(t => t.Name)
            .Select(trip => MapToPublicTripDto(trip))
            .ToList();
    }

    public async Task<IEnumerable<Country>> GetAvailableCountriesForPublicTripsAsync()
    {
        return await _tripRepository.GetCountriesForPublicTripsAsync();
    }

    public int CountAllSpotsInTrip(Trip trip)
    {
        return GetAllSpotsFromTrip(trip).Count();
    }

    public List<string> GetUniqueCountriesFromTrip(Trip trip)
    {
        var countries = new HashSet<string>();

        foreach (var spot in GetAllSpotsFromTrip(trip).Where(s => !string.IsNullOrEmpty(s.CountryName)))
        {
            countries.Add(spot.CountryName!);
        }

        return countries.OrderBy(c => c).ToList();
    }

    // Metoda pomocnicza do pobierania wszystkich miejsc z podróży
    private IEnumerable<Spot> GetAllSpotsFromTrip(Trip trip)
    {
        var spotsFromTrip = trip.Activities.OfType<Spot>();
        var spotsFromDays = trip.Days.SelectMany(d => d.Activities.OfType<Spot>());

        // Łączymy i usuwamy duplikaty po ID
        return spotsFromTrip
            .Concat(spotsFromDays)
            .GroupBy(s => s.Id)
            .Select(g => g.First())
            .ToList();
    }

    private PublicTripDto MapToPublicTripDto(Trip trip)
    {
        var ownerName = !trip.Person!.IsPrivate
            ? $"{trip.Person.FirstName} {trip.Person.LastName}"
            : null;

        return new PublicTripDto
        {
            Id = trip.Id,
            Name = trip.Name,
            StartDate = trip.StartDate,
            EndDate = trip.EndDate,
            OwnerName = ownerName,
            Countries = GetUniqueCountriesFromTrip(trip),
            SpotsCount = CountAllSpotsInTrip(trip),
            ParticipantsCount = trip.Participants?.Count(p => p.Status == TripParticipantStatus.Accepted || p.Status == TripParticipantStatus.Owner) ?? 0
        };
    }

    public async Task MarkAllChecklistItemsAsync(int tripId, bool completed)
    {
        await _tripRepository.MarkAllChecklistItemsAsync(tripId, completed);
    }

    public async Task<Trip> CloneTripAsync(int sourceTripId, string cloningUserId, CloneTripRequestDto request)
    {
        // Pobierz oryginalną wycieczkę z wszystkimi danymi
        var sourceTrip = await _tripRepository.GetByIdAsync(sourceTripId);
        if (sourceTrip == null)
            throw new ArgumentException("Source trip not found");

        if (sourceTrip.IsPrivate)
            throw new InvalidOperationException("Cannot clone private trip");

        // Pobierz kategorie użytkownika klonującego
        var userCategories = await _categoryRepository.GetAllCategoriesByUserAsync(cloningUserId);
        var categoryMap = userCategories.ToDictionary(c => c.Name, c => c.Id);

        // Stwórz nową wycieczkę
        var newTrip = new Trip
        {
            Name = request.Name,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = Status.Planning,
            IsPrivate = request.IsPrivate,
            CurrencyCode = request.TargetCurrency,
            PersonId = cloningUserId,
            Catalog = sourceTrip.Catalog,
            Checklist = CleanChecklist(sourceTrip.Checklist),
            Participants = new List<TripParticipant>(),
            Days = new List<Day>(),
            Activities = new List<Activity>(),
            Transports = new List<Transport>(),
            Expenses = new List<Expense>(),
            ExchangeRates = new List<ExchangeRate>(),
            ChatMessages = new List<ChatMessage>()
        };

        // Zapisz nową wycieczkę
        var savedTrip = await _tripRepository.AddAsync(newTrip);

        try
        {
            // Słowniki do mapowania ID oryginalnych encji na nowe
            var accommodationMap = new Dictionary<int, Accommodation>();
            var spotMap = new Dictionary<int, Spot>();
            var transportMap = new Dictionary<int, Transport>();
            var dayMap = new Dictionary<int, Day>();

            // Klonuj wybrane elementy
            if (request.CloneDays || request.CloneGroups)
            {
                await CloneDaysAndGroupsAsync(sourceTrip, savedTrip, request, cloningUserId, categoryMap, dayMap, spotMap);
            }

            // Klonuj aktywności bez dnia (jeśli wybrano)
            if (request.CloneActivities)
            {
                await CloneActivitiesWithoutDayAsync(sourceTrip, savedTrip, cloningUserId, categoryMap, request.CloneSpots, spotMap);
            }

            // Klonuj zakwaterowania
            if (request.CloneAccommodations)
            {
                await CloneAccommodationsAsync(sourceTrip, savedTrip, cloningUserId, categoryMap, accommodationMap);

                // Przypisz zakwaterowania do odpowiednich dni
                if (request.CloneDays || request.CloneGroups)
                {
                    await AssignAccommodationsToDaysAsync(sourceTrip, savedTrip, accommodationMap, dayMap);
                }
            }

            if (request.CloneTransport)
            {
                await CloneTransportAsync(sourceTrip, savedTrip, cloningUserId, categoryMap, spotMap, transportMap);
            }

            if (request.CloneExpenses)
            {
                await CloneExpensesAsync(sourceTrip, savedTrip, cloningUserId, categoryMap, request.TargetCurrency, spotMap, transportMap);
            }

            return savedTrip;
        }
        catch
        {
            // W przypadku błędu, wycofaj utworzenie wycieczki
            await _tripRepository.DeleteAsync(savedTrip);
            throw;
        }
    }

    public async Task<double> GetDistance(int id, double lat, double lng)
    { 
        var spots = await _spotRepository.GetTripSpotsWithDetailsAsync(id);
        List<(double, double)> coords = spots.Select(s => (s.Latitude, s.Longitude)).ToList();
        double distance = GeoUtils.GetSmallestDistance(coords, (lat, lng));
        return distance;
    }

    private Checklist? CleanChecklist(Checklist? sourceChecklist)
    {
        if (sourceChecklist == null) return new Checklist();

        var cleanChecklist = new Checklist();
        foreach (var item in sourceChecklist.Items)
        {
            var cleanItem = new ChecklistItem
            {
                Title = item.Title,
                IsCompleted = false,
                AssignedParticipantId = null,
                AssignedParticipantName = null
            };
            cleanChecklist.Items.Add(cleanItem);
        }
        return cleanChecklist;
    }

    private async Task CloneDaysAndGroupsAsync(Trip sourceTrip, Trip newTrip, CloneTripRequestDto request, string cloningUserId,
        Dictionary<string, int> categoryMap, Dictionary<int, Day> dayMap, Dictionary<int, Spot> spotMap)
    {
        var sourceDays = await _dayRepository.GetByTripIdAsync(sourceTrip.Id);

        foreach (var sourceDay in sourceDays)
        {
            // Sprawdź czy klonować dzień lub grupę
            bool isDay = sourceDay.Number.HasValue;
            bool isGroup = !string.IsNullOrEmpty(sourceDay.Name);

            if ((isDay && !request.CloneDays) || (isGroup && !request.CloneGroups))
                continue;

            var newDay = new Day
            {
                Number = sourceDay.Number,
                Name = sourceDay.Name,
                Date = sourceDay.Date,
                TripId = newTrip.Id,
                Activities = new List<Activity>(),
                Posts = new List<Post>()
            };

            var savedDay = await _dayRepository.AddAsync(newDay);
            dayMap[sourceDay.Id] = savedDay;

            // Klonuj aktywności jeśli wybrano
            if (request.CloneActivities && sourceDay.Activities.Any())
            {
                await CloneActivitiesAsync(sourceDay.Activities, savedDay, newTrip, cloningUserId, categoryMap, request.CloneSpots, spotMap);
            }
        }
    }

    private async Task CloneActivitiesWithoutDayAsync(Trip sourceTrip, Trip newTrip, string cloningUserId, Dictionary<string, int> categoryMap, bool cloneSpots, Dictionary<int, Spot> spotMap)
    {
        // Pobierz wszystkie aktywności z wycieczki które nie mają przypisanego dnia
        var allActivities = await _activityRepository.GetTripActivitiesWithDetailsAsync(sourceTrip.Id);
        var activitiesWithoutDay = allActivities.Where(a => a.DayId == null).ToList();

        foreach (var sourceActivity in activitiesWithoutDay)
        {
            // Pomijamy Spot i Accommodation w podstawowej pętli
            if (sourceActivity is Spot)
                continue;

            var newActivity = new Activity
            {
                Name = sourceActivity.Name,
                Description = sourceActivity.Description,
                Duration = sourceActivity.Duration,
                Order = sourceActivity.Order,
                StartTime = sourceActivity.StartTime,
                Checklist = CleanChecklist(sourceActivity.Checklist),
                CategoryId = GetMatchingCategoryId(sourceActivity.Category?.Name, categoryMap),
                TripId = newTrip.Id,
                DayId = null
            };

            await _activityRepository.AddAsync(newActivity);
        }

        // Klonuj Spoty i Accommodations jeśli wybrano
        if (cloneSpots)
        {
            var spotsWithoutDay = activitiesWithoutDay.OfType<Spot>().ToList();
            await CloneSpotsAsync(spotsWithoutDay, null, newTrip, cloningUserId, categoryMap, spotMap);
        }
    }

    private async Task CloneActivitiesAsync(ICollection<Activity> sourceActivities, Day newDay, Trip newTrip, string cloningUserId,
        Dictionary<string, int> categoryMap, bool cloneSpots, Dictionary<int, Spot> spotMap)
    {
        foreach (var sourceActivity in sourceActivities)
        {
            // Pomijamy Spot i Accommodation w podstawowej pętli
            if (sourceActivity is Spot)
                continue;

            var newActivity = new Activity
            {
                Name = sourceActivity.Name,
                Description = sourceActivity.Description,
                Duration = sourceActivity.Duration,
                Order = sourceActivity.Order,
                StartTime = sourceActivity.StartTime,
                Checklist = CleanChecklist(sourceActivity.Checklist),
                CategoryId = GetMatchingCategoryId(sourceActivity.Category?.Name, categoryMap),
                TripId = newTrip.Id,
                DayId = newDay.Id
            };

            await _activityRepository.AddAsync(newActivity);
        }

        // Klonuj Spoty i Accommodations jeśli wybrano
        if (cloneSpots)
        {
            await CloneSpotsAsync(sourceActivities, newDay, newTrip, cloningUserId, categoryMap, spotMap);
        }
    }

    private async Task CloneSpotsAsync(IEnumerable<Activity> sourceActivities, Day? newDay, Trip newTrip, string cloningUserId,
        Dictionary<string, int> categoryMap, Dictionary<int, Spot> spotMap)
    {
        var sourceSpots = sourceActivities.OfType<Spot>().ToList();

        foreach (var sourceSpot in sourceSpots)
        {
            // Dla Accommodation - pomiń
            if (sourceSpot is Accommodation)
                continue;

            var newSpot = await CloneSpotAsync(sourceSpot, newDay, newTrip, cloningUserId, categoryMap);

            // Dodaj do mapy
            spotMap[sourceSpot.Id] = newSpot;
        }
    }

    private async Task<Spot> CloneSpotAsync(Spot sourceSpot, Day? newDay, Trip newTrip, string cloningUserId, Dictionary<string, int> categoryMap)
    {
        var newSpot = new Spot
        {
            Name = sourceSpot.Name,
            Description = sourceSpot.Description,
            Duration = sourceSpot.Duration,
            Order = sourceSpot.Order,
            StartTime = sourceSpot.StartTime,
            Checklist = CleanChecklist(sourceSpot.Checklist),
            CategoryId = GetMatchingCategoryId(sourceSpot.Category?.Name, categoryMap),
            TripId = newTrip.Id,
            DayId = newDay?.Id,
            Longitude = sourceSpot.Longitude,
            Latitude = sourceSpot.Latitude,
            Rating = sourceSpot.Rating,
            CountryCode = sourceSpot.CountryCode,
            CountryName = sourceSpot.CountryName,
            Photos = new List<Photo>(),
            Files = new List<Domain.Entities.File>()
        };

        await _spotRepository.AddAsync(newSpot);
        return newSpot;
    }

    private async Task CloneAccommodationsAsync(Trip sourceTrip, Trip newTrip, string cloningUserId, Dictionary<string, int> categoryMap, Dictionary<int, Accommodation> accommodationMap)
    {
        var sourceAccommodations = await _accommodationService.GetTripAccommodationsAsync(sourceTrip.Id);

        foreach (var sourceAccommodation in sourceAccommodations)
        {
            var newAccommodation = new Accommodation
            {
                Name = sourceAccommodation.Name,
                Description = sourceAccommodation.Description,
                Duration = sourceAccommodation.Duration,
                Order = sourceAccommodation.Order,
                StartTime = sourceAccommodation.StartTime,
                Checklist = CleanChecklist(sourceAccommodation.Checklist),
                CategoryId = GetMatchingCategoryId(sourceAccommodation.Category?.Name, categoryMap),
                TripId = newTrip.Id,
                DayId = null, // Tymczasowo bez dnia
                Longitude = sourceAccommodation.Longitude,
                Latitude = sourceAccommodation.Latitude,
                Rating = sourceAccommodation.Rating,
                CountryCode = sourceAccommodation.CountryCode,
                CountryName = sourceAccommodation.CountryName,
                CheckIn = sourceAccommodation.CheckIn,
                CheckOut = sourceAccommodation.CheckOut,
                CheckInTime = sourceAccommodation.CheckInTime,
                CheckOutTime = sourceAccommodation.CheckOutTime,
                Photos = new List<Photo>(),
                Files = new List<Domain.Entities.File>()
            };

            var savedAccommodation = await _accommodationService.AddAsync(newAccommodation);
            accommodationMap[sourceAccommodation.Id] = savedAccommodation;
        }
    }

    private async Task AssignAccommodationsToDaysAsync(Trip sourceTrip, Trip newTrip, Dictionary<int, Accommodation> accommodationMap, Dictionary<int, Day> dayMap)
    {
        var sourceDays = await _dayRepository.GetByTripIdAsync(sourceTrip.Id);

        foreach (var sourceDay in sourceDays)
        {
            if (sourceDay.Accommodation != null && accommodationMap.ContainsKey(sourceDay.Accommodation.Id))
            {
                var newDay = dayMap[sourceDay.Id];
                var newAccommodation = accommodationMap[sourceDay.Accommodation.Id];

                // Znajdź wszystkie dni w nowej wycieczce które pasują do zakresu dat zakwaterowania
                var matchingDays = await TryFindDaysForAccommodation(newTrip.Id, newAccommodation.CheckIn, newAccommodation.CheckOut);

                if (matchingDays != null && matchingDays.Any())
                {
                    await AddAccommodationToDays(matchingDays, newAccommodation.Id);
                }
            }
        }
    }

    private async Task<IEnumerable<Day>> TryFindDaysForAccommodation(int tripId, DateTime checkIn, DateTime checkOut)
    {
        var days = await _dayRepository.GetByTripIdAsync(tripId);
        return days.Where(d => d.Date.Date >= checkIn.Date && d.Date.Date < checkOut.Date);
    }

    private async Task AddAccommodationToDays(IEnumerable<Day> days, int accommodationId)
    {
        foreach (var day in days)
        {
            day.AccommodationId = accommodationId;
            await _dayRepository.UpdateAsync(day);
        }
    }

    private async Task CloneTransportAsync(Trip sourceTrip, Trip newTrip, string cloningUserId, Dictionary<string, int> categoryMap, Dictionary<int, Spot> spotMap, Dictionary<int, Transport> transportMap)
    {
        var sourceTransports = await _transportRepository.GetTransportsByTripIdAsync(sourceTrip.Id);

        // Najpierw upewnij się, że wszystkie spoty używane w transporcie są sklonowane
        foreach (var transport in sourceTransports)
        {
            if (!spotMap.ContainsKey(transport.FromSpotId))
            {
                var newFromSpot = await CloneTransportSpotAsync(transport.FromSpot!, newTrip, cloningUserId, categoryMap);
                spotMap[transport.FromSpotId] = newFromSpot;
            }

            if (!spotMap.ContainsKey(transport.ToSpotId))
            {
                var newToSpot = await CloneTransportSpotAsync(transport.ToSpot!, newTrip, cloningUserId, categoryMap);
                spotMap[transport.ToSpotId] = newToSpot;
            }
        }

        // Teraz sklonuj transporty używając już sklonowanych spotów
        foreach (var sourceTransport in sourceTransports)
        {
            var newTransport = new Transport
            {
                Name = sourceTransport.Name,
                Type = sourceTransport.Type,
                Duration = sourceTransport.Duration,
                TripId = newTrip.Id,
                FromSpotId = spotMap[sourceTransport.FromSpotId].Id,
                ToSpotId = spotMap[sourceTransport.ToSpotId].Id
            };

            var savedTransport = await _transportRepository.AddAsync(newTransport);
            transportMap[sourceTransport.Id] = savedTransport;
        }
    }

    private async Task<Spot> CloneTransportSpotAsync(Spot sourceSpot, Trip newTrip, string cloningUserId, Dictionary<string, int> categoryMap)
    {
        var newSpot = new Spot
        {
            Name = sourceSpot.Name,
            Description = sourceSpot.Description,
            Duration = sourceSpot.Duration,
            Order = sourceSpot.Order,
            StartTime = sourceSpot.StartTime,
            Checklist = CleanChecklist(sourceSpot.Checklist),
            CategoryId = GetMatchingCategoryId(sourceSpot.Category?.Name, categoryMap),
            TripId = newTrip.Id,
            DayId = null, // Spoty transportowe nie są przypisane do dnia
            Longitude = sourceSpot.Longitude,
            Latitude = sourceSpot.Latitude,
            Rating = sourceSpot.Rating,
            CountryCode = sourceSpot.CountryCode,
            CountryName = sourceSpot.CountryName,
            Photos = new List<Photo>(),
            Files = new List<Domain.Entities.File>()
        };

        return await _spotRepository.AddAsync(newSpot);
    }

    private async Task CloneExpensesAsync(Trip sourceTrip, Trip newTrip, string cloningUserId, Dictionary<string, int> categoryMap, CurrencyCode targetCurrency,
        Dictionary<int, Spot> spotMap, Dictionary<int, Transport> transportMap)
    {
        var sourceExpenses = await _expenseRepository.GetByTripIdWithParticipantsAsync(sourceTrip.Id);
        var estimatedExpenses = sourceExpenses.Where(e => e.IsEstimated).ToList();

        // Pobierz kursy wymiany
        var exchangeRates = await GetOrCreateExchangeRatesAsync(sourceTrip, newTrip, targetCurrency);

        foreach (var sourceExpense in estimatedExpenses)
        {
            // Znajdź odpowiedni kurs wymiany
            var sourceCurrency = sourceExpense.ExchangeRate?.CurrencyCodeKey ?? sourceTrip.CurrencyCode;
            var exchangeRate = exchangeRates.FirstOrDefault(er => er.CurrencyCodeKey == sourceCurrency);

            if (exchangeRate == null) continue;

            // Mapuj SpotId i TransportId jeśli istnieją
            int? newSpotId = null;
            int? newTransportId = null;

            if (sourceExpense.SpotId.HasValue && spotMap.ContainsKey(sourceExpense.SpotId.Value))
            {
                newSpotId = spotMap[sourceExpense.SpotId.Value].Id;
            }

            if (sourceExpense.TransportId.HasValue && transportMap.ContainsKey(sourceExpense.TransportId.Value))
            {
                newTransportId = transportMap[sourceExpense.TransportId.Value].Id;
            }

            var newExpense = new Expense
            {
                Name = sourceExpense.Name,
                Value = sourceExpense.Value,
                EstimatedValue = sourceExpense.EstimatedValue,
                IsEstimated = true,
                Multiplier = sourceExpense.Multiplier,
                AdditionalFee = sourceExpense.AdditionalFee,
                PercentageFee = sourceExpense.PercentageFee,
                PaidById = cloningUserId,
                TransferredToId = null,
                CategoryId = GetMatchingCategoryId(sourceExpense.Category?.Name, categoryMap),
                TripId = newTrip.Id,
                ExchangeRateId = exchangeRate.Id,
                SpotId = newSpotId,
                TransportId = newTransportId,
                Participants = new List<ExpenseParticipant>()
            };

            await _expenseRepository.AddAsync(newExpense);
        }
    }

    private async Task<List<ExchangeRate>> GetOrCreateExchangeRatesAsync(Trip sourceTrip, Trip newTrip, CurrencyCode targetCurrency)
    {
        var exchangeRates = new List<ExchangeRate>();

        // Pobierz unikalne waluty z wydatków oryginalnej wycieczki
        var sourceCurrencies = new HashSet<CurrencyCode> {  };
        var sourceExpenses = await _expenseRepository.GetByTripIdWithParticipantsAsync(sourceTrip.Id);

        foreach (var expense in sourceExpenses.Where(e => e.IsEstimated))
        {
            if (expense.ExchangeRate != null)
            {
                sourceCurrencies.Add(expense.ExchangeRate.CurrencyCodeKey);
            }
        }

        // Dla każdej waluty źródłowej pobierz kurs do waluty docelowej
        foreach (var sourceCurrency in sourceCurrencies)
        {
            if (sourceCurrency == targetCurrency)
            {
                // Kurs 1:1 dla tej samej waluty
                var rate = new ExchangeRate
                {
                    CurrencyCodeKey = sourceCurrency,
                    ExchangeRateValue = 1.0m,
                    TripId = newTrip.Id
                };
                var savedRate = await _exchangeRateRepository.AddAsync(rate);
                exchangeRates.Add(savedRate);
            }
            else
            {
                try
                {
                    var rateValue = await _currencyConversionService.GetExchangeRate(sourceCurrency.ToString(), targetCurrency.ToString());

                    var rate = new ExchangeRate
                    {
                        CurrencyCodeKey = sourceCurrency,
                        ExchangeRateValue = rateValue,
                        TripId = newTrip.Id
                    };
                    var savedRate = await _exchangeRateRepository.AddAsync(rate);
                    exchangeRates.Add(savedRate);
                }
                catch
                {
                    // W przypadku błędu pobierania kursu, pomiń tę walutę
                    continue;
                }
            }
        }

        return exchangeRates;
    }

    private int? GetMatchingCategoryId(string? categoryName, Dictionary<string, int> categoryMap)
    {
        if (string.IsNullOrEmpty(categoryName)) return null;

        return categoryMap.ContainsKey(categoryName) ? categoryMap[categoryName] : null;
    }
}