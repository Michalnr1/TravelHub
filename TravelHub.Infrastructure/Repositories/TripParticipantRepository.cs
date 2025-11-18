using Microsoft.EntityFrameworkCore;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;

namespace TravelHub.Infrastructure.Repositories;

public class TripParticipantRepository : GenericRepository<TripParticipant>, ITripParticipantRepository
{
    public TripParticipantRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<TripParticipant>> GetByTripIdAsync(int tripId)
    {
        return await _context.TripParticipants
            .Include(tp => tp.Person)
                .ThenInclude(p => p.Friends)
            .Include(tp => tp.Trip)
            .Where(tp => tp.TripId == tripId)
            .ToListAsync();
    }

    public async Task<IEnumerable<TripParticipant>> GetByPersonIdAsync(string personId)
    {
        return await _context.TripParticipants
            .Include(tp => tp.Trip)
            .ThenInclude(t => t!.Person)
            .Where(tp => tp.PersonId == personId)
            .ToListAsync();
    }

    public async Task<TripParticipant?> GetByTripAndPersonAsync(int tripId, string personId)
    {
        return await _context.TripParticipants
            .Include(tp => tp.Person)
            .Include(tp => tp.Trip)
            .FirstOrDefaultAsync(tp => tp.TripId == tripId && tp.PersonId == personId);
    }

    public async Task<bool> ExistsAsync(int tripId, string personId)
    {
        return await _context.TripParticipants
            .AnyAsync(tp => tp.TripId == tripId && tp.PersonId == personId);
    }

    public async Task<int> GetAcceptedParticipantsCountAsync(int tripId)
    {
        return await _context.TripParticipants
            .CountAsync(tp => tp.TripId == tripId && (tp.Status == TripParticipantStatus.Accepted || tp.Status == TripParticipantStatus.Owner));
    }

    public async Task<IEnumerable<TripParticipant>> GetPendingInvitationsAsync(string personId)
    {
        return await _context.TripParticipants
            .Include(tp => tp.Trip)
            .ThenInclude(t => t!.Person)
            .Where(tp => tp.PersonId == personId && tp.Status == TripParticipantStatus.Pending)
            .ToListAsync();
    }

    public async Task<IEnumerable<TripParticipant>> GetUserParticipatingTripsAsync(string userId)
    {
        return await _context.TripParticipants
            .Include(tp => tp.Trip)
            .ThenInclude(t => t!.Days)
            .Include(tp => tp.Trip)
            .ThenInclude(t => t!.Participants)
            .Where(tp => tp.PersonId == userId && tp.Status == TripParticipantStatus.Accepted)
            .ToListAsync();
    }

    public async Task<bool> HasAcceptedParticipantAsync(int tripId, string userId)
    {
        return await _context.TripParticipants
            .AnyAsync(tp => tp.TripId == tripId &&
                           tp.PersonId == userId &&
                           tp.Status == TripParticipantStatus.Accepted);
    }

    public async Task<bool> IsUserTripOwnerAsync(int tripId, string userId)
    {
        var trip = await _context.Trips
            .Where(t => t.Id == tripId)
            .Select(t => t.PersonId == userId)
            .FirstOrDefaultAsync();

        return trip;
    }

    public async Task<IEnumerable<Person>> GetAllTripParticipantsAsync(int tripId)
    {
        return await _context.TripParticipants
            .Include(tp => tp.Person)
            .Where(tp => tp.TripId == tripId)
            .Select(t => t.Person)
            .ToListAsync();
    }
}