using Microsoft.EntityFrameworkCore;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;

namespace TravelHub.Infrastructure.Repositories;

public class AccommodationRepository : GenericRepository<Accommodation>, IAccommodationRepository
{
    public AccommodationRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Accommodation>> GetTripAccommodationsAsync(int tripId)
    {
        return await _context.Set<Accommodation>()
            .Where(a => a.TripId == tripId)
            .OrderBy(a => a.CheckIn)
            .ToListAsync();
    }

    public async Task<Accommodation?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.Set<Accommodation>()
            .Where(a => a.Id == id)
            .Include(a => a.Trip)
            .Include(a => a.Category)
            .Include(a => a.Day)
            .Include(a => a.Photos)
            .Include(a => a.Expense)
            .FirstOrDefaultAsync();
    }
}
