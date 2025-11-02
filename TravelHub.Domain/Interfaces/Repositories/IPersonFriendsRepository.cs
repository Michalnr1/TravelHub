using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Repositories;

public interface IPersonFriendsRepository : IGenericRepository<PersonFriends>
{
    Task<PersonFriends?> GetFriendshipAsync(string userId, string friendId);
    Task<IReadOnlyList<PersonFriends>> GetFriendshipsAsync(string userId);
    Task<bool> FriendshipExistsAsync(string user1Id, string user2Id);
    Task RemoveFriendshipAsync(string userId, string friendId);
    Task<int> GetFriendsCountAsync(string userId);
}