using System.ComponentModel.DataAnnotations;
using TravelHub.Domain.Entities;
using TravelHub.Web.ViewModels.Activities;

namespace TravelHub.Web.ViewModels.Blog;

public class BlogViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Catalog { get; set; }
    public BlogVisibility Visibility { get; set; }
    public int TripId { get; set; }
    public string TripName { get; set; } = string.Empty;
    public required string OwnerId { get; set; }
    public List<PostViewModel> Posts { get; set; } = new();
}

public class CreateBlogViewModel
{
    public int TripId { get; set; }
    public string TripName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Blog name is required")]
    [StringLength(100, ErrorMessage = "Blog name cannot be longer than 100 characters")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot be longer than 500 characters")]
    public string? Description { get; set; }

    [StringLength(50, ErrorMessage = "Catalog cannot be longer than 50 characters")]
    public string? Catalog { get; set; }

    [Required(ErrorMessage = "Please select blog visibility")]
    [Display(Name = "Blog Visibility")]
    public BlogVisibility Visibility { get; set; } = BlogVisibility.Private;
}

public class EditBlogViewModel
{
    public int Id { get; set; }
    public int TripId { get; set; }
    public string TripName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Blog name is required")]
    [StringLength(100, ErrorMessage = "Blog name cannot be longer than 100 characters")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot be longer than 500 characters")]
    public string? Description { get; set; }

    [StringLength(50, ErrorMessage = "Catalog cannot be longer than 50 characters")]
    public string? Catalog { get; set; }

    [Required(ErrorMessage = "Blog visibility is required")]
    [Display(Name = "Blog Visibility")]
    public BlogVisibility Visibility { get; set; }
}

public class PostViewModel
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
    public List<CommentViewModel> Comments { get; set; } = new();
    public List<PhotoViewModel> Photos { get; set; } = new();
    public IFormFileCollection? NewPhotos { get; set; }

    // Właściwości dla zaplanowanych postów
    public bool IsScheduled { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public DateTime? PublishedDate { get; set; }

    // Obliczana właściwość dla statusu
    public string Status
    {
        get
        {
            return IsScheduled ? "Scheduled" : "Published";
        }
    }

    // Obliczana właściwość - czy post jest opublikowany
    public bool IsPublished
    {
        get
        {
            return !IsScheduled || PublishedDate.HasValue;
        }
    }
}

public class CreatePostViewModel
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, ErrorMessage = "Title cannot be longer than 200 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Content is required")]
    public string Content { get; set; } = string.Empty;

    public int BlogId { get; set; }

    public int? DayId { get; set; }

    public IFormFileCollection? Photos { get; set; }

    // Właściwości dla planowania
    public bool IsScheduled { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public DateTimeOffset? ScheduledForDateTimeOffset { get; set; }

    // Status postu (tylko do odczytu w widoku edycji)
    public bool IsCurrentlyScheduled { get; set; }
    public DateTime? CurrentScheduledFor { get; set; }
    public DateTime? PublishedDate { get; set; }

    // Lista dni
    public List<DaySelectItem> Days { get; set; } = new();
}

public class CommentViewModel
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreationDate { get; set; }
    public DateTime? EditDate { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorId { get; set; } = string.Empty;
    public int PostId { get; set; }
    public List<PhotoViewModel> Photos { get; set; } = new();
    public IFormFileCollection? NewPhotos { get; set; }
}

public class CreateCommentViewModel
{
    public string Content { get; set; } = string.Empty;
    public int PostId { get; set; }
    public IFormFileCollection? Photos { get; set; }
}

public class EditCommentViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Content is required")]
    [StringLength(1000, ErrorMessage = "Comment cannot be longer than 1000 characters")]
    public string Content { get; set; } = string.Empty;

    public int PostId { get; set; }
    public string PostTitle { get; set; } = string.Empty;
    public List<PhotoViewModel> ExistingPhotos { get; set; } = new();
    public IFormFileCollection? NewPhotos { get; set; }
}

public class PhotoViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Alt { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public DateTime? UploadDate { get; set; }
    public int? PostId { get; set; }
    public int? CommentId { get; set; }
}