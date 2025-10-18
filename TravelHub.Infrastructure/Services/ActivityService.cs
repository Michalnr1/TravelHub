using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;

namespace TravelHub.Infrastructure.Services;

public class ActivityService : GenericService<Activity>, IActivityService
{
    private readonly IActivityRepository _activityRepository;

    public ActivityService(IActivityRepository activityRepository)
        : base(activityRepository)
    {
        _activityRepository = activityRepository;
    }

    public async Task<IReadOnlyList<Activity>> GetOrderedDailyActivitiesAsync(int dayId)
    {
        return await _activityRepository.GetActivitiesByDayIdAsync(dayId);
    }

    public async Task<decimal> CalculateDailyActivityDurationAsync(int dayId)
    {
        var activities = await _activityRepository.GetActivitiesByDayIdAsync(dayId);

        if (activities == null || !activities.Any())
        {
            return 0;
        }

        return activities.Sum(a => a.Duration);
    }

    public async Task ReorderActivitiesAsync(int dayId, List<(int activityId, int newOrder)> orderUpdates)
    {
        var activities = await _activityRepository.GetActivitiesByDayIdAsync(dayId);

        foreach (var update in orderUpdates)
        {
            var activityToUpdate = activities.FirstOrDefault(a => a.Id == update.activityId);
            if (activityToUpdate != null && activityToUpdate.DayId == dayId)
            {
                activityToUpdate.Order = update.newOrder;
                await UpdateAsync(activityToUpdate);
            }
        }
    }
}