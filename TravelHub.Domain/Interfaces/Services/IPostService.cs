using Microsoft.AspNetCore.Http;
using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Services;

public interface IPostService : IGenericService<Post>
{
    Task<Post?> GetWithDetailsAsync(int id);
    Task<IReadOnlyList<Post>> GetByBlogIdAsync(int blogId);
    Task<Post> CreatePostAsync(Post post, IFormFileCollection? photos, string webRootPath);
    Task<bool> CanUserCreatePostAsync(int blogId, string userId);
    Task<bool> CanUserEditPostAsync(int postId, string userId);
    Task<int?> GetTripIdByPostIdAsync(int postId);
    Task<int?> GetBlogIdByPostIdAsync(int postId);
}
