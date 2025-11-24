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
            .Include(a => (a as Spot)!.Expense)
            .Include(a => a.Category)
            .OrderBy(a => a.Order)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Activity>> GetTripActivitiesWithDetailsAsync(int tripId)
    {
        return await _context.Set<Activity>()
            .Where(a => a.TripId == tripId)
            .Include(a => a.Category)
            .Include(a => a.Day)
            .Include(a => a.Trip)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Activity>> GetAllWithDetailsAsync()
    {
        return await _context.Set<Activity>()
            .Include(a => a.Category)
            .Include(a => a.Trip)
            .Include(a => a.Day)
            .ToListAsync();
    }

    public new async Task<Activity?> GetByIdAsync(object id)
    {
        return await _context.Set<Activity>()
            .Include(a => a.Category)
            .Include(a => a.Trip)
            .Include(a => a.Day)
            .FirstOrDefaultAsync(a => a.Id == (int)id);
    }

    public async Task<Activity?> GetByIdWithTripAndParticipantsAsync(int activityId)
    {
        return await _context.Activities
            .Include(a => a.Category)
            .Include(a => a.Trip)
                .ThenInclude(t => t!.Participants)
                    .ThenInclude(tp => tp.Person)
            .Include(a => a.Day)
            .FirstOrDefaultAsync(a => a.Id == activityId);
    }
}
