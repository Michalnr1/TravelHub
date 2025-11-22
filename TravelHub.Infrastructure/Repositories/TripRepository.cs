using Microsoft.EntityFrameworkCore;
using TravelHub.Domain.Entities;
using TravelHub.Domain.DTOs;
using TravelHub.Domain.Interfaces.Repositories;

namespace TravelHub.Infrastructure.Repositories;

public class TripRepository : GenericRepository<Trip>, ITripRepository
{
    public TripRepository(ApplicationDbContext context) : base(context)
    {
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
            .Include(t => t.Days)
                .ThenInclude(d => d.Activities)
            .Include(t => t.Activities)
            .Include(t => t.Transports)
            .Include(t => t.Person)
                .ThenInclude(p => p!.Friends)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<IEnumerable<Trip>> GetByUserIdAsync(string userId)
    {
        return await _context.Set<Trip>()
            .Where(t => t.PersonId == userId)
            .Include(t => t.Days)
            .OrderByDescending(t => t.StartDate)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Set<Trip>().AnyAsync(t => t.Id == id);
    }

    public async Task<IReadOnlyList<Trip>> GetAllWithUserAsync()
    {
        return await _context.Set<Trip>()
            .Include(t => t.Person)
            .OrderByDescending(t => t.StartDate)
            .ToListAsync();
    }

    public async Task AddCountryToTrip(int id, Country country)
    {
        //Trip? trip = await _context.Set<Trip>().Include(t => t.Countries).FirstOrDefaultAsync(t => t.Id == id);
        //if (trip != null)
        //{   
        //    if (!trip.Countries.Contains(country))
        //    {
        //        trip.Countries.Add(country);
        //        await _context.SaveChangesAsync();
        //    }      
        //}
        throw new NotImplementedException();
    }

    public async Task<Trip?> GetByIdWithParticipantsAsync(int id)
    {
        return await _context.Trips
            .Include(t => t.Participants)
                .ThenInclude(tp => tp.Person)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<IEnumerable<Trip>> GetPublicTripsAsync()
    {
        //return await _context.Trips
        //    .Include(t => t.Person)
        //    .Include(t => t.Days)
        //        .ThenInclude(d => d.Activities.OfType<Spot>())
        //    .Include(t => t.Activities.OfType<Spot>())
        //    .Include(t => t.Participants)
        //    .Where(t => !t.IsPrivate)
        //    .OrderBy(t => t.Name)
        //    .ToListAsync();
        return await _context.Trips
            .Include(t => t.Person)
            .Include(t => t.Days)
                .ThenInclude(d => d.Activities)
            .Include(t => t.Activities)
            .Include(t => t.Participants)
            .Where(t => !t.IsPrivate)
            .OrderBy(t => t.Name)
            .AsSplitQuery()
            //.AsNoTracking() // Opcjonalnie - lepsza wydajność dla danych tylko do odczytu
            .ToListAsync();
    }

    public async Task<IEnumerable<Country>> GetCountriesForPublicTripsAsync()
    {
        return await _context.Trips
            .Where(t => !t.IsPrivate)
            .SelectMany(t => t.Activities.OfType<Spot>()
                .Union(t.Days.SelectMany(d => d.Activities.OfType<Spot>())))
            .Where(s => s.Country != null)
            .Select(s => s.Country!)
            .Distinct()
            .OrderBy(c => c.Name)
            .ToListAsync();
    }
}