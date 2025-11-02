using Microsoft.EntityFrameworkCore;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;

namespace TravelHub.Infrastructure.Repositories;

public class PersonFriendsRepository : GenericRepository<PersonFriends>, IPersonFriendsRepository
{
    public PersonFriendsRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<PersonFriends?> GetFriendshipAsync(string userId, string friendId)
    {
        return await _context.PersonFriends
            .Include(pf => pf.User)
            .Include(pf => pf.Friend)
            .FirstOrDefaultAsync(pf => pf.UserId == userId && pf.FriendId == friendId);
    }

    public async Task<bool> FriendshipExistsAsync(string user1Id, string user2Id)
    {
        return await _context.PersonFriends
            .AnyAsync(pf => 
                (pf.UserId == user1Id && pf.FriendId == user2Id) ||
                (pf.UserId == user2Id && pf.FriendId == user1Id));
    }

    public async Task<IReadOnlyList<PersonFriends>> GetFriendshipsAsync(string userId)
    {
        return await _context.PersonFriends
            .Include(pf => pf.Friend)
            .Where(pf => pf.UserId == userId)
            .OrderByDescending(pf => pf.CreatedAt)
            .ToListAsync();
    }

    public async Task RemoveFriendshipAsync(string userId, string friendId)
    {
        var friendship1 = await GetFriendshipAsync(userId, friendId);
        var friendship2 = await GetFriendshipAsync(friendId, userId);

        if (friendship1 != null)
        {
            await DeleteAsync(friendship1);
        }

        if (friendship2 != null)
        {
            await DeleteAsync(friendship2);
        }
    }

    public async Task<int> GetFriendsCountAsync(string userId)
    {
        return await _context.PersonFriends
            .CountAsync(pf => pf.UserId == userId);
    }
}