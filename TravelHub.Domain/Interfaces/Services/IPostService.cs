using Microsoft.AspNetCore.Http;
using TravelHub.Domain.DTOs;
using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Services;

public interface IPostService : IGenericService<Post>
{
    Task<Post?> GetWithDetailsAsync(int id);
    Task<IReadOnlyList<Post>> GetByBlogIdAsync(int blogId);
    Task<Post> CreatePostAsync(Post post, IFormFileCollection? photos, string webRootPath, bool isScheduled = false, DateTimeOffset? scheduledFor = null);
    Task<bool> CanUserCreatePostAsync(int blogId, string userId);
    Task<bool> CanUserEditPostAsync(int postId, string userId);
    Task<int?> GetTripIdByPostIdAsync(int postId);
    Task<int?> GetBlogIdByPostIdAsync(int postId);
    Task<IReadOnlyList<Post>> GetScheduledPostsAsync(int blogId);
    Task<IReadOnlyList<Post>> GetPublishedPostsAsync(int blogId);
    Task PublishScheduledPostAsync(int postId);
    Task<bool> UpdateScheduledPostAsync(int postId, UpdateScheduledPostDto updateDto, IFormFileCollection? photos, string webRootPath);
    Task<bool> CancelScheduledPostAsync(int postId);
}
