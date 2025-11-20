using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;

namespace TravelHub.Infrastructure.Services;

public class BlogService : GenericService<Blog>, IBlogService
{
    private readonly IBlogRepository _blogRepository;
    private readonly ITripParticipantRepository _tripParticipantRepository;

    public BlogService(IBlogRepository repository, ITripParticipantRepository tripParticipantRepository)
        : base(repository)
    {
        _blogRepository = repository;
        _tripParticipantRepository = tripParticipantRepository;
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
}
