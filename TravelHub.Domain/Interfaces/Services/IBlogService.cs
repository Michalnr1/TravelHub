using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Services;

public interface IBlogService : IGenericService<Blog>
{
    Task<Blog?> GetByTripIdAsync(int tripId);
    Task<Blog?> GetWithPostsAsync(int id);
    Task<IReadOnlyList<Blog>> GetByOwnerIdAsync(string ownerId);
    Task<bool> CanUserAccessBlogAsync(int blogId, string userId);
}