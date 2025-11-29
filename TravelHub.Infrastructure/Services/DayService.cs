using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Infrastructure.Migrations;

namespace TravelHub.Infrastructure.Services;

public class DayService : GenericService<Day>, IDayService
{
    private readonly IDayRepository _dayRepository;
    private readonly IActivityService _activityService;
    private readonly ITripService _tripService;

    public DayService(IDayRepository dayRepository, ITripService tripService, IActivityService activityService)
        : base(dayRepository)
    {
        _dayRepository = dayRepository;
        _tripService = tripService;
        _activityService = activityService;
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

        var allSpots = day.Activities.OfType<Spot>().ToList();

        if (day.Accommodation != null)
            allSpots.Add(day.Accommodation);

        var trip = await _tripService.GetTripWithDetailsAsync(day.TripId);

        if (trip != null)
        {
            var previousDay = trip.Days!.Where(d => d.Number == day!.Number - 1).FirstOrDefault();
            if (previousDay != null && previousDay.Accommodation != null)
            {
                allSpots.Add(previousDay.Accommodation);
            } 
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

    public async Task<Activity?> CheckNewForCollisions(int id, string startTimeString, string? durationString)
    {
        var activities = await _activityService.GetOrderedDailyActivitiesAsync(id);
        decimal startTime = ConvertTimeStringToDecimal(startTimeString);
        decimal endTime = startTime + ConvertTimeStringToDecimal(durationString);
        foreach (var activity in activities)
        {
            if (activity.StartTime != null)
            {
                decimal otherStartTime = activity.StartTime.Value;
                decimal otherEndTime = otherStartTime + activity.Duration;

                if ((otherStartTime < startTime && startTime < otherEndTime)
                    || (otherStartTime < endTime && endTime < otherEndTime)
                    || (startTime < otherStartTime && otherStartTime < endTime))
                {
                    return activity;
                }
            }
        }
        return null;
    }

    private decimal ConvertTimeStringToDecimal(string? timeString)
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
}
