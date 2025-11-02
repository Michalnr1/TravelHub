using TravelHub.Domain.DTOs;
using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Services;

public interface IFriendRequestService
{
    Task<FriendRequestResult> SendFriendRequestAsync(string requesterId, string addresseeIdentifier, string? message = null);
    Task<FriendRequestResult> AcceptFriendRequestAsync(int friendRequestId, string userId);
    Task<FriendRequestResult> DeclineFriendRequestAsync(int friendRequestId, string userId);
    Task<FriendRequestResult> CancelFriendRequestAsync(int friendRequestId, string userId);
    Task<IReadOnlyList<FriendRequest>> GetPendingRequestsAsync(string userId);
    Task<IReadOnlyList<FriendRequest>> GetSentRequestsAsync(string userId);
    Task<IReadOnlyList<Person>> GetPublicUsersAsync(string currentUserId);
    Task<bool> HasPendingRequestAsync(string user1Id, string user2Id);
}
