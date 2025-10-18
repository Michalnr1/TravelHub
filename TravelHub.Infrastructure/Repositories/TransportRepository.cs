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
            // Dołączamy powiązane encje
            .Include(t => t.FromSpot)
            .Include(t => t.ToSpot)
            .OrderBy(t => t.Id) // Sortowanie według kryterium
            .ToListAsync();
    }
}
