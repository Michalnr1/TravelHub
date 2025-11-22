using System.Diagnostics.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Infrastructure.Repositories;

namespace TravelHub.Infrastructure.Services;

public class TripService : GenericService<Trip>, ITripService
{
    private readonly ITripRepository _tripRepository;
    private readonly IDayRepository _dayRepository;
    private readonly IAccommodationService _accommodationService;
    private readonly ITripParticipantRepository _tripParticipantRepository;
    private readonly IBlogRepository _blogRepository;
    private readonly ILogger<TripService> _logger;

    public TripService(ITripRepository tripRepository,
        IDayRepository dayRepository,
        IAccommodationService accommodationService,
        ITripParticipantRepository tripParticipantRepository,
        IBlogRepository blogRepository,
        ILogger<TripService> logger)
        : base(tripRepository)
    {
        _tripRepository = tripRepository;
        _dayRepository = dayRepository;
        _accommodationService = accommodationService;
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
}