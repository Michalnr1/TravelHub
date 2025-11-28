using TravelHub.Domain.DTOs;
using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Services;

public interface IBlogService : IGenericService<Blog>
{
    Task<Blog?> GetByTripIdAsync(int tripId);
    Task<Blog?> GetWithPostsAsync(int id);
    Task<IReadOnlyList<Blog>> GetByOwnerIdAsync(string ownerId);
    Task<bool> CanUserAccessBlogAsync(int blogId, string userId);
    Task<bool> CanUserCommentOnBlogAsync(int blogId, string userId);
    Task<List<PublicBlogInfoDto>> GetPublicBlogsAsync();
    Task<List<CountryWithBlogCountDto>> GetCountriesWithPublicBlogsAsync();
    Task<List<PublicBlogInfoDto>> GetAccessibleBlogsAsync(string? userId = null);
    Task<List<CountryWithBlogCountDto>> GetCountriesWithAccessibleBlogsAsync(string? userId = null);
    Task<List<PublicBlogInfoDto>> GetFriendsBlogsAsync(string userId);
}