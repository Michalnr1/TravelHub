using Microsoft.EntityFrameworkCore;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;

namespace TravelHub.Infrastructure.Repositories;

public class DayRepository : GenericRepository<Day>, IDayRepository
{
    public DayRepository(ApplicationDbContext context) : base(context) 
    {
    }

    public async Task<Day?> GetByIdWithActivitiesAsync(int id)
    {
        return await _context.Set<Day>()
            .Include(d => d.Trip)
            .Include(d => d.Activities)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<IEnumerable<Day>> GetByTripIdAsync(int tripId)
    {
        return await _context.Set<Day>()
            .Where(d => d.TripId == tripId)
            .Include(d => d.Activities)
            .OrderBy(d => d.Number)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Set<Day>().AnyAsync(d => d.Id == id);
    }
}