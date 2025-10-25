using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;

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
}
