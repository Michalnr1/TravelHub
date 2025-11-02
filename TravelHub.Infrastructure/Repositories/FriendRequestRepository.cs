using Microsoft.EntityFrameworkCore;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;

namespace TravelHub.Infrastructure.Repositories;

public class FriendRequestRepository : GenericRepository<FriendRequest>, IFriendRequestRepository
{
    public FriendRequestRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<FriendRequest?> GetFriendRequestAsync(string requesterId, string addresseeId)
    {
        return await _context.FriendRequests
            .Include(f => f.Requester)
            .Include(f => f.Addressee)
            .FirstOrDefaultAsync(f => f.RequesterId == requesterId && f.AddresseeId == addresseeId);
    }

    public async Task<bool> FriendRequestExistsAsync(string user1Id, string user2Id)
    {
        return await _context.FriendRequests
            .AnyAsync(f =>
                (f.RequesterId == user1Id && f.AddresseeId == user2Id && f.Status == FriendRequestStatus.Pending) ||
                (f.RequesterId == user2Id && f.AddresseeId == user1Id && f.Status == FriendRequestStatus.Pending));
    }

    public async Task<bool> HasPendingRequestAsync(string user1Id, string user2Id)
    {
        return await _context.FriendRequests
            .AnyAsync(f =>
                f.RequesterId == user1Id &&
                f.AddresseeId == user2Id &&
                f.Status == FriendRequestStatus.Pending);
    }

    public async Task<IReadOnlyList<FriendRequest>> GetPendingRequestsAsync(string userId)
    {
        return await _context.FriendRequests
            .Include(f => f.Requester)
            .Where(f => f.AddresseeId == userId && f.Status == FriendRequestStatus.Pending)
            .OrderByDescending(f => f.RequestedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<FriendRequest>> GetSentRequestsAsync(string userId)
    {
        return await _context.FriendRequests
            .Include(f => f.Addressee)
            .Where(f => f.RequesterId == userId && f.Status == FriendRequestStatus.Pending)
            .OrderByDescending(f => f.RequestedAt)
            .ToListAsync();
    }
}