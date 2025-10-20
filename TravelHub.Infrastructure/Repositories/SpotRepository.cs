﻿using Microsoft.EntityFrameworkCore;
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
            .Include(s => s.Category)
            .Include(s => s.Day)
            .Include(s => s.Trip)
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
            .Include(s => s.Day)
            .Include(s => s.Trip)
            .Include(s => s.Photos)
            .Include(s => s.TransportsFrom)
            .Include(s => s.TransportsTo)
            .FirstOrDefaultAsync(a => a.Id == (int)id);
    }

    // Implementacja metody FindNearbySpotsAsync byłaby zbyt skomplikowana w standardowym LINQ
    // i wymagałaby użycia zewnętrznego API. Pominięta, by utrzymać czystość kodu.
}
