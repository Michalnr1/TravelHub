using TravelHub.Domain.Entities;

namespace TravelHub.Web.ViewModels.Blog;

public class PublicBlogsViewModel
{
    public List<PublicBlogItemViewModel> Blogs { get; set; } = new();
    public List<CountryViewModel> AvailableCountries { get; set; } = new();
    public string? SelectedCountryCode { get; set; }
    public string? SearchTerm { get; set; }
    public bool ShowFriendsOnly { get; set; }
    public int TotalBlogs { get; set; }
    public int TotalPosts { get; set; }
    public int TotalComments { get; set; }
    public int TotalCountries { get; set; }
    public int FriendsBlogsCount { get; set; }
}

public class PublicBlogItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Catalog { get; set; }
    public string TripName { get; set; } = string.Empty;
    public int TripId { get; set; }
    public int PostsCount { get; set; }
    public int CommentsCount { get; set; }
    public List<string> Countries { get; set; } = new();
    public int? LatestPostId { get; set; }
    public DateTime? LatestPostDate { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public bool IsFriend { get; set; }
    public BlogVisibility Visibility { get; set; }
    public List<string> FriendParticipants { get; set; } = new();
    public bool HasFriendParticipants => FriendParticipants.Any();
}

public class CountryViewModel
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int BlogCount { get; set; }
}

public class PublicBlogViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Catalog { get; set; }
    public string TripName { get; set; } = string.Empty;
    public int TripId { get; set; }
    public BlogVisibility Visibility { get; set; } = BlogVisibility.Private;
    public List<PublicPostViewModel> Posts { get; set; } = new();
}

public class PublicPostViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreationDate { get; set; }
    public DateTime? EditDate { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorId { get; set; } = string.Empty;
    public int BlogId { get; set; }
    public int? DayId { get; set; }
    public string? DayName { get; set; }
    public List<PhotoViewModel> Photos { get; set; } = new();
    public List<PublicCommentViewModel> Comments { get; set; } = new();
}

public class PublicCommentViewModel
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreationDate { get; set; }
    public DateTime? EditDate { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorId { get; set; } = string.Empty;
    public int PostId { get; set; }
    public List<PhotoViewModel> Photos { get; set; } = new();
}