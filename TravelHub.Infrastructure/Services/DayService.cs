using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Infrastructure.Migrations;

namespace TravelHub.Infrastructure.Services;

public class DayService : GenericService<Day>, IDayService
{
    private readonly IDayRepository _dayRepository;
    private readonly ITripService _tripService;

    public DayService(IDayRepository dayRepository, ITripService tripService)
        : base(dayRepository)
    {
        _dayRepository = dayRepository;
        _tripService = tripService;
    }

    public async Task<Day?> GetDayWithDetailsAsync(int id)
    {
        return await _dayRepository.GetByIdWithActivitiesAsync(id);
    }

    public async Task<Day?> GetDayByIdAsync(int id)
    {
        return await _dayRepository.GetByIdAsync(id);
    }

    public async Task<bool> UserOwnsDayAsync(int dayId, string userId)
    {
        var day = await _dayRepository.GetByIdWithTripAsync(dayId);
        return day?.Trip?.PersonId == userId;
    }

    public async Task<bool> IsDayAGroupAsync(int dayId)
    {
        var day = await _dayRepository.GetByIdAsync(dayId);
        return day?.Number == null;
    }

    public async Task<bool> ValidateDateRangeAsync(int tripId, DateTime date)
    {
        var trip = await _tripService.GetByIdAsync(tripId);
        if (trip == null) return false;

        return date >= trip.StartDate && date <= trip.EndDate;
    }

    public async Task<IEnumerable<Day>> GetDaysByTripIdAsync(int tripId)
    {
        return await _dayRepository.GetByTripIdAsync(tripId);
    }

    public async Task AddAccommodationToDay(int dayId, int accommodationId)
    {
        var day = await _dayRepository.GetByIdAsync(dayId);
        day!.AccommodationId = accommodationId;

        await _dayRepository.UpdateAsync(day);
    }

    public async Task<(double medianLatitude, double medianLongitude)> GetMedianCoords(int id)
    {
        var day = await GetDayWithDetailsAsync(id);

        if (day == null)
        {
            throw new ArgumentException($"Day with ID {id} not found");
        }

        var allSpots = day.Activities.OfType<Spot>();

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
}
