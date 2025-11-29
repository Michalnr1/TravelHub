using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.Xml;
using TravelHub.Domain.DTOs;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;

namespace TravelHub.Infrastructure.Repositories;

public class TripRepository : GenericRepository<Trip>, ITripRepository
{
    private readonly ITransportRepository _transportRepository;
    private readonly ISpotRepository _spotRepository;

    public TripRepository(ApplicationDbContext context, ITransportRepository transportRepository, ISpotRepository spotRepository) : base(context)
    {
        _transportRepository = transportRepository;
        _spotRepository = spotRepository;
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
            .Include(t => t.Participants)
                .ThenInclude(tp => tp.Person)
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

    public async Task<Trip?> GetByIdWithParticipantsAsync(int id)
    {
        return await _context.Trips
            .Include(t => t.Participants)
                .ThenInclude(tp => tp.Person)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<IEnumerable<Trip>> GetPublicTripsAsync()
    {
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

    public async Task MarkAllChecklistItemsAsync(int tripId, bool completed)
    {
        var trip = await _context.Trips
            .Include(t => t.Checklist)
            .ThenInclude(c => c.Items)
            .FirstOrDefaultAsync(t => t.Id == tripId);

        if (trip == null)
            throw new KeyNotFoundException($"Trip with ID {tripId} not found.");

        if (trip.Checklist?.Items != null)
        {
            foreach (var item in trip.Checklist.Items)
            {
                item.IsCompleted = completed;
            }

            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(int tripId)
    {
        var trip = await _context.Trips
            .Include(t => t.Days)
                .ThenInclude(d => d.Activities)
            .Include(t => t.Activities)
            .Include(t => t.Transports)
                .ThenInclude(tr => tr.Expense)
            .Include(t => t.Expenses)
            .Include(t => t.ExchangeRates)
            .Include(t => t.Participants)
            .Include(t => t.ChatMessages)
            .Include(t => t.Blog)
                .ThenInclude(b => b.Posts)
                    .ThenInclude(p => p.Comments)
            .Include(t => t.Blog)
                .ThenInclude(b => b.Posts)
                    .ThenInclude(p => p.Photos)
            .FirstOrDefaultAsync(t => t.Id == tripId);

        if (trip == null) return;

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 0. Rozwiąż problem Accommodation <-> Day CYCLE
            await ResolveAccommodationDayCycleAsync(tripId);

            // 1. Usuń wszystkie Transporty i ich Expenses
            await DeleteTripTransportsAsync(tripId);

            // 2. Usuń wszystkie Spot-y (Activities) i ich Expenses
            await DeleteTripSpotsAsync(tripId);

            // 3. Usuń pozostałe Expenses (te niepowiązane z Spot/Transport)
            var remainingExpenses = await _context.Expenses
                .Where(e => e.TripId == tripId)
                .ToListAsync();
            _context.Expenses.RemoveRange(remainingExpenses);

            // 4. Usuń Blog i powiązane Posty, Komentarze, Zdjęcia
            if (trip.Blog != null)
            {
                // Usuń komentarze do postów
                foreach (var post in trip.Blog.Posts)
                {
                    _context.Comments.RemoveRange(post.Comments);
                    _context.Photos.RemoveRange(post.Photos);
                }

                // Usuń posty
                _context.Posts.RemoveRange(trip.Blog.Posts);

                // Usuń blog
                _context.Blogs.Remove(trip.Blog);
            }

            // 5. Na końcu usuń Trip
            _context.Trips.Remove(trip);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // Metoda do rozwiązania cyklu Accommodation <-> Day
    private async Task ResolveAccommodationDayCycleAsync(int tripId)
    {
        // Znajdź wszystkie days z accommodation w tej wycieczce
        var daysWithAccommodation = await _context.Days
            .Where(d => d.TripId == tripId && d.AccommodationId != null)
            .ToListAsync();

        // Tymczasowo ustaw AccommodationId na null aby rozwiązać cykl
        foreach (var day in daysWithAccommodation)
        {
            day.AccommodationId = null;
        }

        await _context.SaveChangesAsync();
    }

    // Pomocnicza metoda do usuwania Spot-ów w wycieczce
    private async Task DeleteTripSpotsAsync(int tripId)
    {
        var spots = await _context.Set<Spot>()
            .Where(s => s.TripId == tripId)
            .ToListAsync();

        foreach (var spot in spots)
        {
            await _transportRepository.DeleteAsync(spot.Id);
        }
    }

    // Pomocnicza metoda do usuwania Transportów w wycieczce
    private async Task DeleteTripTransportsAsync(int tripId)
    {
        var transports = await _context.Transports
            .Where(t => t.TripId == tripId)
            .ToListAsync();

        foreach (var transport in transports)
        {
            await _transportRepository.DeleteAsync(transport.Id);
        }
    }
}