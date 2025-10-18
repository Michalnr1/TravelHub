using Microsoft.EntityFrameworkCore;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;

namespace TravelHub.Infrastructure.Repositories;

public class DayRepository : IDayRepository
{
    private readonly ApplicationDbContext _context;

    public DayRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Day?> GetByIdAsync(int id)
    {
        return await _context.Days
            .Include(d => d.Trip)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<Day?> GetByIdWithActivitiesAsync(int id)
    {
        return await _context.Days
            .Include(d => d.Trip)
            .Include(d => d.Activities)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<IEnumerable<Day>> GetByTripIdAsync(int tripId)
    {
        return await _context.Days
            .Include(d => d.Activities)
            .Where(d => d.TripId == tripId)
            .OrderBy(d => d.Number)
            .ToListAsync();
    }

    public async Task AddAsync(Day day)
    {
        await _context.Days.AddAsync(day);
    }

    public void Update(Day day)
    {
        _context.Days.Update(day);
    }

    public void Delete(Day day)
    {
        _context.Days.Remove(day);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Days.AnyAsync(d => d.Id == id);
    }
}
