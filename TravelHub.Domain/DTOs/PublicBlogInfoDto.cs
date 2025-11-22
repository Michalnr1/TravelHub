using TravelHub.Domain.Entities;

namespace TravelHub.Domain.DTOs;

public class PublicBlogInfoDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Catalog { get; set; }
    public string TripName { get; set; } = string.Empty;
    public int TripId { get; set; }
    public int PostsCount { get; set; }
    public int CommentsCount { get; set; }
    public List<Country> Countries { get; set; } = new();
    public int? LatestPostId { get; set; }
    public DateTime? LatestPostDate { get; set; }
}
