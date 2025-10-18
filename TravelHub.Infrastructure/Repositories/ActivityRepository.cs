using Microsoft.EntityFrameworkCore;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;

namespace TravelHub.Infrastructure.Repositories;

public class ActivityRepository : GenericRepository<Activity>, IActivityRepository
{
    public ActivityRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Activity>> GetActivitiesByDayIdAsync(int dayId)
    {
        return await _context.Set<Activity>()
            .Where(a => a.DayId == dayId)
            .Include(a => a.Category)
            .OrderBy(a => a.Order)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Activity>> GetTripActivitiesWithDetailsAsync(int tripId)
    {
        return await _context.Set<Activity>()
            .Where(a => a.TripId == tripId)
            .Include(a => a.Category)
            .ToListAsync();
    }
}
