using Microsoft.AspNetCore.Identity;
using TravelHub.Domain.DTOs;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;

namespace TravelHub.Infrastructure.Services;

public class BlogService : GenericService<Blog>, IBlogService
{
    private readonly IBlogRepository _blogRepository;
    private readonly ITripParticipantRepository _tripParticipantRepository;
    private readonly IFriendshipService _friendshipService;
    private readonly ISpotService _spotService;
    private readonly UserManager<Person> _userManager;

    public BlogService(
        IBlogRepository repository,
        ITripParticipantRepository tripParticipantRepository,
        IFriendshipService friendshipService,
        ISpotService spotService,
        UserManager<Person> userManager)
        : base(repository)
    {
        _blogRepository = repository;
        _tripParticipantRepository = tripParticipantRepository;
        _friendshipService = friendshipService;
        _spotService = spotService;
        _userManager = userManager;
    }

    public async Task<Blog?> GetByTripIdAsync(int tripId)
    {
        return await _blogRepository.GetByTripIdAsync(tripId);
    }

    public async Task<Blog?> GetWithPostsAsync(int id)
    {
        return await _blogRepository.GetWithPostsAsync(id);
    }

    public async Task<IReadOnlyList<Blog>> GetByOwnerIdAsync(string ownerId)
    {
        return await _blogRepository.GetByOwnerIdAsync(ownerId);
    }

    public async Task<bool> CanUserAccessBlogAsync(int blogId, string userId)
    {
        var blog = await _blogRepository.GetByIdAsync(blogId);
        if (blog == null || blog.Visibility == BlogVisibility.Private) return false;

        // Public blogs are accessible to everyone
        if (blog.Visibility == BlogVisibility.Public)
            return true;

        // Check if user is trip participant
        var isTripParticipant = await _tripParticipantRepository.ExistsAsync(blog.TripId, userId);
        if (isTripParticipant)
            return true;

        // For friend-based visibilities, check friendship
        if (blog.Visibility == BlogVisibility.ForMyFriends || blog.Visibility == BlogVisibility.ForTripParticipantsFriends)
        {
            if (blog.Visibility == BlogVisibility.ForMyFriends)
            {
                // Check if user is friend of blog owner
                return await _friendshipService.IsFriendAsync(blog.OwnerId, userId);
            }
            else if (blog.Visibility == BlogVisibility.ForTripParticipantsFriends)
            {
                // Check if user is friend of any trip participant
                var participants = await _tripParticipantRepository.GetByTripIdAsync(blog.TripId);
                foreach (var participant in participants)
                {
                    if (await _friendshipService.IsFriendAsync(participant.PersonId, userId))
                        return true;
                }
            }
        }

        return false;
    }

    public async Task<bool> CanUserCommentOnBlogAsync(int blogId, string userId)
    {
        // For now, anyone who can access the blog can comment
        return await CanUserAccessBlogAsync(blogId, userId);
    }

    public async Task<List<PublicBlogInfoDto>> GetAccessibleBlogsAsync(string? userId = null)
    {
        var blogs = await _blogRepository.GetAllAsync();
        var accessibleBlogs = new List<Blog>();

        foreach (var blog in blogs)
        {
            // Public blogs are always accessible
            if (blog.Visibility == BlogVisibility.Public)
            {
                accessibleBlogs.Add(blog);
                continue;
            }

            // For non-public blogs, check access if user is authenticated
            if (userId != null && await CanUserAccessBlogAsync(blog.Id, userId))
            {
                accessibleBlogs.Add(blog);
            }
        }

        return await MapToPublicBlogInfoDto(accessibleBlogs, userId);
    }

    public async Task<List<PublicBlogInfoDto>> GetFriendsBlogsAsync(string userId)
    {
        var allBlogs = await _blogRepository.GetAllAsync();
        var friendsBlogs = new List<Blog>();

        // Pobierz listę ID znajomych użytkownika
        var friends = await _friendshipService.GetFriendsAsync(userId);
        var friendIds = friends.Select(f => f.Id).ToList();

        foreach (var blog in allBlogs)
        {
            // Sprawdź czy użytkownik ma dostęp do bloga
            if (await CanUserAccessBlogAsync(blog.Id, userId))
            {
                // Sprawdź czy blog należy do znajomego LUB znajomy jest uczestnikiem tej wycieczki
                var isFriendOwner = friendIds.Contains(blog.OwnerId);
                var isFriendParticipant = await IsFriendParticipantInTripAsync(blog.TripId, friendIds);

                if (isFriendOwner || isFriendParticipant)
                {
                    friendsBlogs.Add(blog);
                }
            }
        }

        return await MapToPublicBlogInfoDto(friendsBlogs, userId);
    }

    private async Task<bool> IsFriendParticipantInTripAsync(int tripId, List<string> friendIds)
    {
        var participants = await _tripParticipantRepository.GetByTripIdAsync(tripId);
        return participants.Any(p => friendIds.Contains(p.PersonId));
    }

    public async Task<List<PublicBlogInfoDto>> GetPublicBlogsAsync()
    {
        var publicBlogs = await _blogRepository.GetPublicBlogsWithDetailsAsync();
        return await MapToPublicBlogInfoDto(publicBlogs.ToList(), null);
    }

    public async Task<List<CountryWithBlogCountDto>> GetCountriesWithAccessibleBlogsAsync(string? userId = null)
    {
        var accessibleBlogs = await GetAccessibleBlogsAsync(userId);

        var countryCounts = accessibleBlogs
            .SelectMany(b => b.Countries)
            .GroupBy(c => new { c.Code, c.Name })
            .Select(g => new CountryWithBlogCountDto
            {
                Code = g.Key.Code,
                Name = g.Key.Name,
                BlogCount = g.Count()
            })
            .OrderByDescending(c => c.BlogCount)
            .ThenBy(c => c.Name)
            .ToList();

        return countryCounts;
    }

    public async Task<List<CountryWithBlogCountDto>> GetCountriesWithPublicBlogsAsync()
    {
        return await GetCountriesWithAccessibleBlogsAsync(null);
    }

    private async Task<List<PublicBlogInfoDto>> MapToPublicBlogInfoDto(List<Blog> blogs, string? userId)
    {
        var result = new List<PublicBlogInfoDto>();

        // Pobierz listę znajomych jeśli użytkownik jest zalogowany
        List<string> friendIds = new();
        if (userId != null)
        {
            var friends = await _friendshipService.GetFriendsAsync(userId);
            friendIds = friends.Select(f => f.Id).ToList();
        }

        foreach (var blog in blogs)
        {
            // Get countries for this trip
            var countries = await _spotService.GetCountriesByTripAsync(blog.TripId);

            // Get latest post
            var latestPost = blog.Posts.OrderByDescending(p => p.CreationDate).FirstOrDefault();

            // Check if owner is friend of current user
            bool isFriendOwner = userId != null && friendIds.Contains(blog.OwnerId);

            // Get friend participants in this trip
            var friendParticipants = new List<string>();
            if (userId != null)
            {
                var participants = await _tripParticipantRepository.GetByTripIdAsync(blog.TripId);
                var friendParticipantIds = participants
                    .Where(p => friendIds.Contains(p.PersonId) && p.PersonId != blog.OwnerId)
                    .Select(p => p.PersonId)
                    .ToList();

                // Pobierz imiona i nazwiska znajomych uczestników
                foreach (var participantId in friendParticipantIds)
                {
                    var participant = await _userManager.FindByIdAsync(participantId);
                    if (participant != null)
                    {
                        friendParticipants.Add($"{participant.FirstName} {participant.LastName}");
                    }
                }
            }

            result.Add(new PublicBlogInfoDto
            {
                Id = blog.Id,
                Name = blog.Name,
                Description = blog.Description,
                Catalog = blog.Catalog,
                TripName = blog.Trip?.Name ?? "Unknown Trip",
                TripId = blog.TripId,
                PostsCount = blog.Posts.Count,
                CommentsCount = blog.Posts.Sum(p => p.Comments.Count),
                Countries = countries.ToList(),
                LatestPostId = latestPost?.Id,
                LatestPostDate = latestPost?.CreationDate,
                OwnerName = $"{blog.Owner?.FirstName} {blog.Owner?.LastName}",
                OwnerId = blog.OwnerId,
                IsFriend = isFriendOwner,
                Visibility = blog.Visibility,
                FriendParticipants = friendParticipants // Nowa właściwość
            });
        }

        return result;
    }
}