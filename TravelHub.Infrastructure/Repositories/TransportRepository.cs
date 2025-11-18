using Microsoft.EntityFrameworkCore;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;

namespace TravelHub.Infrastructure.Repositories;

public class TransportRepository : GenericRepository<Transport>, ITransportRepository
{
    public TransportRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Transport>> GetTransportsByTripIdAsync(int tripId)
    {
        return await _context.Set<Transport>()
            .Where(t => t.TripId == tripId)
            .Include(t => t.Trip)
            .Include(t => t.FromSpot)
            .Include(t => t.ToSpot)
            .ToListAsync();
    }

    public async Task<Transport?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.Set<Transport>()
            .Where(t => t.Id == id)
            .Include(t => t.Trip)
            .Include(t => t.FromSpot)
            .Include(t => t.ToSpot)
            .Include(t => t.Expense)
                .ThenInclude(e => e!.ExchangeRate)
            .FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<Transport>> GetAllWithDetailsAsync()
    {
        return await _context.Set<Transport>()
            .Include(t => t.Trip)
            .Include(t => t.FromSpot)
            .Include(t => t.ToSpot)
            .ToListAsync();
    }
}
