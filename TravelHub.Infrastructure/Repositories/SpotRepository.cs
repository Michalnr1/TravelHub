using Microsoft.EntityFrameworkCore;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;

namespace TravelHub.Infrastructure.Repositories;

public class SpotRepository : GenericRepository<Spot>, ISpotRepository
{
    public SpotRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Spot?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.Set<Spot>()
            .Where(s => s.Id == id)
            .Include(s => s.Photos)
            .Include(s => s.TransportsFrom)
            .Include(s => s.TransportsTo)
            .FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<Spot>> GetSpotsUsedInTripTransportsAsync(int tripId)
    {
        var spots = await _context.Set<Spot>()
            .Where(s => s.TransportsFrom.Any(t => t.TripId == tripId) ||
                        s.TransportsTo.Any(t => t.TripId == tripId))
            .ToListAsync();

        return spots;
    }

    public async Task<IReadOnlyList<Spot>> GetByTripIdAsync(int tripId)
    {
        return await _context.Set<Spot>()
            .Where(s => s.TripId == tripId)
            .ToListAsync();
    }

    // Implementacja metody FindNearbySpotsAsync byłaby zbyt skomplikowana w standardowym LINQ
    // i wymagałaby użycia zewnętrznego API. Pominięta, by utrzymać czystość kodu.
}
