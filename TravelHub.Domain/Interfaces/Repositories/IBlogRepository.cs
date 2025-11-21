using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Repositories;

public interface IBlogRepository : IGenericRepository<Blog>
{
    Task<Blog?> GetByTripIdAsync(int tripId);
    Task<Blog?> GetWithPostsAsync(int id);
    Task<IReadOnlyList<Blog>> GetByOwnerIdAsync(string ownerId);
    Task<IReadOnlyList<Blog>> GetPublicBlogsWithDetailsAsync();
}