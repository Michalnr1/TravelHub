using Microsoft.EntityFrameworkCore;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;

namespace TravelHub.Infrastructure.Repositories;

public class TripRepository : ITripRepository
{
    private readonly ApplicationDbContext _context;

    public TripRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Trip?> GetByIdAsync(int id)
    {
        return await _context.Trips
            .Include(t => t.Person)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Trip?> GetByIdWithDaysAsync(int id)
    {
        return await _context.Trips
            .Include(t => t.Person)
            .Include(t => t.Days)
                .ThenInclude(d => d.Activities)
            .Include(t => t.Transports)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<IEnumerable<Trip>> GetByUserIdAsync(string userId)
    {
        return await _context.Trips
            .Include(t => t.Person)
            .Include(t => t.Days)
            .Where(t => t.PersonId == userId)
            .OrderByDescending(t => t.StartDate)
            .ToListAsync();
    }

    public async Task AddAsync(Trip trip)
    {
        await _context.Trips.AddAsync(trip);
    }

    public void Update(Trip trip)
    {
        _context.Trips.Update(trip);
    }

    public void Delete(Trip trip)
    {
        _context.Trips.Remove(trip);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Trips.AnyAsync(t => t.Id == id);
    }
}