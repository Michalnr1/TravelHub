using Microsoft.EntityFrameworkCore;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;

namespace TravelHub.Infrastructure.Repositories;

public class FlightInfoRepository : IFlightInfoRepository
{
    private readonly ApplicationDbContext _context;

    public FlightInfoRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FlightInfo?> GetByIdAsync(int id)
    {
        return await _context.FlightInfos
            .Include(f => f.Trip)
            .Include(f => f.AddedBy)
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<IEnumerable<FlightInfo>> GetByTripIdAsync(int tripId)
    {
        return await _context.FlightInfos
            .Include(f => f.AddedBy)
            .Where(f => f.TripId == tripId)
            .OrderBy(f => f.DepartureTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<FlightInfo>> GetByTripAndUserAsync(int tripId, string userId)
    {
        return await _context.FlightInfos
            .Include(f => f.AddedBy)
            .Where(f => f.TripId == tripId && f.PersonId == userId)
            .OrderBy(f => f.DepartureTime)
            .ToListAsync();
    }

    public async Task<FlightInfo> AddAsync(FlightInfo flightInfo)
    {
        _context.FlightInfos.Add(flightInfo);
        await _context.SaveChangesAsync();
        return flightInfo;
    }

    public async Task UpdateAsync(FlightInfo flightInfo)
    {
        _context.FlightInfos.Update(flightInfo);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(FlightInfo flightInfo)
    {
        _context.FlightInfos.Remove(flightInfo);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.FlightInfos.AnyAsync(f => f.Id == id);
    }
}
