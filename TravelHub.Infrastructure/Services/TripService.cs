using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;

namespace TravelHub.Infrastructure.Services;

public class TripService : GenericService<Trip>, ITripService
{
    // Repozytoria są potrzebne dla metod specyficznych
    private readonly ITripRepository _tripRepository;
    private readonly IDayRepository _dayRepository;

    public TripService(ITripRepository tripRepository, IDayRepository dayRepository)
        : base(tripRepository)
    {
        _tripRepository = tripRepository;
        _dayRepository = dayRepository;
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
        if (existingDays.Any(d => d.Number == day.Number))
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
}