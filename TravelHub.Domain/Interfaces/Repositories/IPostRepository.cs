using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Repositories;

public interface IPostRepository : IGenericRepository<Post>
{
    Task<Post?> GetByIdWithDetailsAsync(int id);
    Task<IReadOnlyList<Post>> GetByBlogIdAsync(int blogId);
    Task<IReadOnlyList<Post>> GetPostsByAuthorIdAsync(string authorId);
    Task<IReadOnlyList<Post>> GetRecentPostsAsync(int count);
    Task<IReadOnlyList<Post>> GetByDayIdAsync(int dayId);
    Task<int?> GetTripIdByPostIdAsync(int postId);
    Task<int?> GetBlogIdByPostIdAsync(int postId);
    Task<IReadOnlyList<Post>> GetScheduledPostsByBlogIdAsync(int blogId);
    Task<IReadOnlyList<Post>> GetPublishedPostsByBlogIdAsync(int blogId);
}
