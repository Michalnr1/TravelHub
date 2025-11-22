using Microsoft.AspNetCore.Http;
using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Services;

public interface ICommentService : IGenericService<Comment>
{
    Task<IReadOnlyList<Comment>> GetByPostIdAsync(int postId);
    Task<Comment> CreateCommentAsync(Comment comment, IFormFileCollection? photos, string webRootPath);
    Task<Comment> UpdateCommentAsync(Comment comment, IFormFileCollection? newPhotos, string webRootPath);
    Task<bool> CanUserCreateCommentAsync(int postId, string userId);
    Task<bool> CanUserEditCommentAsync(int commentId, string userId);
}
