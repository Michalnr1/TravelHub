using TravelHub.Domain.DTOs;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;

namespace TravelHub.Infrastructure.Services;

public class BlogService : GenericService<Blog>, IBlogService
{
    private readonly IBlogRepository _blogRepository;
    private readonly ITripParticipantRepository _tripParticipantRepository;
    private readonly ISpotService _spotService;

    public BlogService(IBlogRepository repository, ITripParticipantRepository tripParticipantRepository, ISpotService spotService)
        : base(repository)
    {
        _blogRepository = repository;
        _tripParticipantRepository = tripParticipantRepository;
        _spotService = spotService;
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
        if (blog == null) return false;

        var tripParticipants = await _tripParticipantRepository.GetByTripIdAsync(blog.TripId);

        return tripParticipants.Any(tp => tp.PersonId == userId);
    }

    public async Task<List<PublicBlogInfoDto>> GetPublicBlogsAsync()
    {
        var publicBlogs = await _blogRepository.GetPublicBlogsWithDetailsAsync();

        var result = new List<PublicBlogInfoDto>();

        foreach (var blog in publicBlogs)
        {
            // Get countries for this trip
            var countries = await _spotService.GetCountriesByTripAsync(blog.TripId);

            // Get latest post
            var latestPost = blog.Posts.OrderByDescending(p => p.CreationDate).FirstOrDefault();

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
                LatestPostDate = latestPost?.CreationDate
            });
        }

        return result;
    }

    public async Task<List<CountryWithBlogCountDto>> GetCountriesWithPublicBlogsAsync()
    {
        var publicBlogs = await GetPublicBlogsAsync();

        var countryCounts = publicBlogs
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
}
