using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Repositories;

public interface IFriendRequestRepository : IGenericRepository<FriendRequest>
{
    Task<FriendRequest?> GetFriendRequestAsync(string requesterId, string addresseeId);
    Task<IReadOnlyList<FriendRequest>> GetPendingRequestsAsync(string userId);
    Task<IReadOnlyList<FriendRequest>> GetSentRequestsAsync(string userId);
    Task<bool> FriendRequestExistsAsync(string user1Id, string user2Id);
    Task<bool> HasPendingRequestAsync(string user1Id, string user2Id);
}