using TravelHub.Domain.DTOs;
using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Services;

public interface IFriendshipService
{
    Task<IReadOnlyList<Person>> GetFriendsAsync(string userId);
    Task<FriendshipResult> RemoveFriendAsync(string userId, string friendId);
    Task<bool> IsFriendAsync(string user1Id, string user2Id);
    Task<int> GetFriendsCountAsync(string userId);
}
