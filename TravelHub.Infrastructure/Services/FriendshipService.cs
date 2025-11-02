using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using TravelHub.Domain.DTOs;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;

namespace TravelHub.Infrastructure.Services;
public class FriendshipService : IFriendshipService
{
    private readonly IPersonFriendsRepository _personFriendsRepository;
    private readonly UserManager<Person> _userManager;
    private readonly ILogger<FriendshipService> _logger;

    public FriendshipService(
        IPersonFriendsRepository personFriendsRepository,
        UserManager<Person> userManager,
        ILogger<FriendshipService> logger)
    {
        _personFriendsRepository = personFriendsRepository;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Person>> GetFriendsAsync(string userId)
    {
        var friendships = await _personFriendsRepository.GetFriendshipsAsync(userId);
        return friendships
            .Select(pf => pf.Friend)
            .ToList();
    }

    public async Task<FriendshipResult> RemoveFriendAsync(string userId, string friendId)
    {
        try
        {
            await _personFriendsRepository.RemoveFriendshipAsync(userId, friendId);

            _logger.LogInformation("Friendship removed between {UserId} and {FriendId}", userId, friendId);

            return new FriendshipResult { Success = true, Message = "Friend removed successfully." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing friendship between {UserId} and {FriendId}", userId, friendId);
            return new FriendshipResult { Success = false, Message = "An error occurred while removing friend." };
        }
    }

    public async Task<bool> IsFriendAsync(string user1Id, string user2Id)
    {
        return await _personFriendsRepository.FriendshipExistsAsync(user1Id, user2Id);
    }

    public async Task<int> GetFriendsCountAsync(string userId)
    {
        return await _personFriendsRepository.GetFriendsCountAsync(userId);
    }
}