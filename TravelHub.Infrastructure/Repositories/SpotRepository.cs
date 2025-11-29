using Microsoft.EntityFrameworkCore;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;

namespace TravelHub.Infrastructure.Repositories;

public class SpotRepository : GenericRepository<Spot>, ISpotRepository
{
    private readonly ITransportRepository _transportRepository;

    public SpotRepository(ApplicationDbContext context, ITransportRepository transportRepository) : base(context)
    {
        _transportRepository = transportRepository;
    }

    public async Task<Spot?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.Set<Spot>()
            .Where(s => s.Id == id)
            .Include(s => s.Category)
            .Include(s => s.Day)
            .Include(s => s.Trip)
                .ThenInclude(t => t!.Participants)
                    .ThenInclude(tp => tp.Person)
            .Include(s => s.Expense)
                .ThenInclude(e => e!.ExchangeRate)
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

    public async Task<IReadOnlyList<Spot>> GetTripSpotsWithDetailsAsync(int tripId)
    {
        return await _context.Set<Spot>()
            .Where(s => s.TripId == tripId)
            .Include(s => s.Category)
            .Include(s => s.Country)
            .Include(s => s.Day)
            .Include(s => s.Trip)
            .Include(s => s.Photos)
            .Include(s => s.TransportsFrom)
            .Include(s => s.TransportsTo)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Spot>> GetAllWithDetailsAsync()
    {
        return await _context.Set<Spot>()
            .Include(s => s.Category)
            .Include(s => s.Country)
            .Include(s => s.Day)
            .Include(s => s.Trip)
            .Include(s => s.Photos)
            .Include(s => s.TransportsFrom)
            .Include(s => s.TransportsTo)
            .ToListAsync();
    }

    public new async Task<Spot?> GetByIdAsync(object id)
    {
        return await _context.Set<Spot>()
            .Include(s => s.Category)
            .Include(s => s.Country)
            .Include(s => s.Day)
            .Include(s => s.Trip)
            .Include(s => s.Photos)
            .Include(s => s.TransportsFrom)
            .Include(s => s.TransportsTo)
            .FirstOrDefaultAsync(a => a.Id == (int)id);
    }

    // Implementacja metody FindNearbySpotsAsync byłaby zbyt skomplikowana w standardowym LINQ
    // i wymagałaby użycia zewnętrznego API. Pominięta, by utrzymać czystość kodu.

    public async Task<IReadOnlyList<Country>> GetCountriesByTripAsync(int tripId)
    {
        var countries = await _context.Set<Spot>()
            .Include(s => s.Country)
                .ThenInclude(c => c!.Spots)
            .Where(s => s.TripId == tripId && s.Country != null)
            .Select(s => s.Country!)
            .GroupBy(c => c.Name)
            .Select(g => g.First())
            .ToListAsync();

        return countries;
    }

    public async Task DeleteAsync(int id)
    {
        var spot = await _context.Set<Spot>()
            .Include(s => s.Expense)
            .Include(s => s.TransportsFrom)
            .Include(s => s.TransportsTo)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (spot != null)
        {
            // Obsłuż powiązane expenses
            if (spot.Expense != null)
            {
                _context.Expenses.Remove(spot.Expense);
            }

            // Obsłuż powiązane transports
            foreach (var transport in spot.TransportsFrom.ToList())
            {
                await _transportRepository.DeleteAsync(transport.Id);
            }

            foreach (var transport in spot.TransportsTo.ToList())
            {
                await _transportRepository.DeleteAsync(transport.Id);
            }

            _context.Set<Spot>().Remove(spot);
            await _context.SaveChangesAsync();
        }
    }
}
