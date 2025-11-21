using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Web.ViewModels.Activities;
using TravelHub.Web.ViewModels.Blog;
using PhotoViewModel = TravelHub.Web.ViewModels.Blog.PhotoViewModel;

namespace TravelHub.Web.Controllers;

[Authorize]
public class BlogController : Controller
{
    private readonly IBlogService _blogService;
    private readonly IPostService _postService;
    private readonly ICommentService _commentService;
    private readonly IPhotoService _photoService;
    private readonly IDayService _dayService;
    private readonly ITripService _tripService;
    private readonly ITripParticipantService _tripParticipantService;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly UserManager<Person> _userManager;
    private readonly ILogger<BlogController> _logger;

    public BlogController(IBlogService blogService, IPostService postService,
        ICommentService commentService, IPhotoService photoService, IDayService dayService,
        ITripService tripService, ITripParticipantService tripParticipantService,
        IWebHostEnvironment webHostEnvironment, UserManager<Person> userManager, ILogger<BlogController> logger)
    {
        _blogService = blogService;
        _postService = postService;
        _commentService = commentService;
        _photoService = photoService;
        _dayService = dayService;
        _tripService = tripService;
        _tripParticipantService = tripParticipantService;
        _webHostEnvironment = webHostEnvironment;
        _userManager = userManager;
        _logger = logger;
    }

    // GET: Blog/5
    [HttpGet]
    public async Task<IActionResult> Index(int tripId)
    {
        var currentUserId = _userManager.GetUserId(User);

        // Sprawdź czy użytkownik ma dostęp do tripa
        if (!await _tripParticipantService.UserHasAccessToTripAsync(tripId, currentUserId!))
        {
            return Forbid();
        }

        // Sprawdź czy blog istnieje
        var blog = await _blogService.GetByTripIdAsync(tripId);

        if (blog == null)
        {
            // Sprawdź czy użytkownik jest właścicielem tripa
            var isOwner = await _tripService.UserOwnsTripAsync(tripId, currentUserId!);

            if (isOwner)
            {
                // Przekieruj do akcji tworzenia bloga
                return RedirectToAction("CreateBlog", new { tripId });
            }
            else
            {
                // Użytkownik nie jest właścicielem - pokaż informację
                TempData["InfoMessage"] = "This trip doesn't have a blog yet. Only the trip owner can create one.";
                return RedirectToAction("Details", "Trips", new { id = tripId });
            }
        }

        // Blog istnieje - pokaż normalny widok
        var viewModel = new BlogViewModel
        {
            Id = blog.Id,
            Name = blog.Name,
            Description = blog.Description,
            Catalog = blog.Catalog,
            IsPrivate = blog.IsPrivate,
            TripId = blog.TripId,
            TripName = blog.Trip?.Name ?? string.Empty,
            OwnerId = blog.OwnerId,
            Posts = blog.Posts.Select(p => new PostViewModel
            {
                Id = p.Id,
                Title = p.Title,
                Content = p.Content,
                CreationDate = p.CreationDate,
                EditDate = p.EditDate,
                AuthorName = $"{p.Author?.FirstName} {p.Author?.LastName}",
                AuthorId = p.AuthorId,
                BlogId = p.BlogId,
                DayId = p.DayId,
                DayName = p.Day?.Name,
                Comments = p.Comments.OrderBy(c => c.CreationDate).Select(c => new CommentViewModel
                {
                    Id = c.Id,
                    Content = c.Content,
                    CreationDate = c.CreationDate,
                    EditDate = c.EditDate,
                    AuthorName = $"{c.Author?.FirstName} {c.Author?.LastName}",
                    AuthorId = c.AuthorId,
                    PostId = c.PostId,
                    Photos = c.Photos.Select(ph => new PhotoViewModel
                    {
                        Id = ph.Id,
                        Name = ph.Name,
                        Alt = ph.Alt,
                        FilePath = ph.FilePath
                    }).ToList()
                }).ToList(),
                Photos = p.Photos.Select(ph => new PhotoViewModel
                {
                    Id = ph.Id,
                    Name = ph.Name,
                    Alt = ph.Alt,
                    FilePath = ph.FilePath
                }).ToList()
            }).ToList()
        };

        return View(viewModel);
    }

    // GET: Blog/CreateBlog/5
    [HttpGet]
    public async Task<IActionResult> CreateBlog(int tripId)
    {
        var currentUserId = _userManager.GetUserId(User);

        // Sprawdź czy użytkownik jest właścicielem tripa
        if (!await _tripService.UserOwnsTripAsync(tripId, currentUserId!))
        {
            TempData["ErrorMessage"] = "Only the trip owner can create a blog.";
            return RedirectToAction("Details", "Trips", new { id = tripId });
        }

        // Sprawdź czy blog już istnieje
        var existingBlog = await _blogService.GetByTripIdAsync(tripId);
        if (existingBlog != null)
        {
            return RedirectToAction("Index", new { tripId });
        }

        var trip = await _tripService.GetByIdAsync(tripId);
        var viewModel = new CreateBlogViewModel
        {
            TripId = tripId,
            TripName = trip?.Name ?? "Unknown Trip",
            Name = $"{trip?.Name} - Blog",
            Description = $"Blog for trip: {trip?.Name}"
        };

        return View(viewModel);
    }

    // POST: Blog/CreateBlog/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBlog(CreateBlogViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var currentUserId = _userManager.GetUserId(User);

        // Sprawdź czy użytkownik jest właścicielem tripa
        if (!await _tripService.UserOwnsTripAsync(model.TripId, currentUserId!))
        {
            TempData["ErrorMessage"] = "Only the trip owner can create a blog.";
            return RedirectToAction("Details", "Trips", new { id = model.TripId });
        }

        // Sprawdź czy blog już istnieje
        var existingBlog = await _blogService.GetByTripIdAsync(model.TripId);
        if (existingBlog != null)
        {
            return RedirectToAction("Index", new { tripId = model.TripId });
        }

        var blog = new Blog
        {
            Name = model.Name,
            Description = model.Description,
            Catalog = model.Catalog,
            IsPrivate = model.IsPrivate,
            OwnerId = currentUserId!,
            TripId = model.TripId
        };

        await _blogService.AddAsync(blog);
        TempData["SuccessMessage"] = "Blog created successfully!";

        return RedirectToAction("Index", new { tripId = model.TripId });
    }

    // GET: Blog/EditBlog/5
    [HttpGet]
    public async Task<IActionResult> EditBlog(int id)
    {
        var blog = await _blogService.GetByIdAsync(id);
        if (blog == null)
        {
            return NotFound();
        }

        var currentUserId = _userManager.GetUserId(User);
        if (blog.OwnerId != currentUserId)
        {
            return Forbid();
        }

        var viewModel = new EditBlogViewModel
        {
            Id = blog.Id,
            Name = blog.Name,
            Description = blog.Description,
            Catalog = blog.Catalog,
            IsPrivate = blog.IsPrivate,
            TripId = blog.TripId,
            TripName = blog.Trip?.Name ?? "Unknown Trip"
        };

        return View(viewModel);
    }

    // POST: Blog/EditBlog/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditBlog(int id, EditBlogViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existingBlog = await _blogService.GetByIdAsync(id);
        if (existingBlog == null)
        {
            return NotFound();
        }

        var currentUserId = _userManager.GetUserId(User);
        if (existingBlog.OwnerId != currentUserId)
        {
            return Forbid();
        }

        try
        {
            existingBlog.Name = model.Name;
            existingBlog.Description = model.Description;
            existingBlog.Catalog = model.Catalog;
            existingBlog.IsPrivate = model.IsPrivate;

            await _blogService.UpdateAsync(existingBlog);

            TempData["Success"] = "Blog updated successfully!";
            return RedirectToAction("Index", new { tripId = existingBlog.TripId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating blog {BlogId}", id);
            ModelState.AddModelError("", "An error occurred while updating the blog.");
            return View(model);
        }
    }

    // GET: Blog/Post/5
    [HttpGet]
    public async Task<IActionResult> Post(int id)
    {
        var post = await _postService.GetWithDetailsAsync(id);
        if (post == null)
        {
            return NotFound();
        }

        var currentUserId = _userManager.GetUserId(User);
        if (!await _blogService.CanUserAccessBlogAsync(post.BlogId, currentUserId!))
        {
            return Forbid();
        }

        var viewModel = new PostViewModel
        {
            Id = post.Id,
            Title = post.Title,
            Content = post.Content,
            CreationDate = post.CreationDate,
            EditDate = post.EditDate,
            AuthorName = $"{post.Author?.FirstName} {post.Author?.LastName}",
            AuthorId = post.AuthorId,
            BlogId = post.BlogId,
            DayId = post.DayId,
            DayName = post.Day?.Name,
            Photos = post.Photos.Select(ph => new PhotoViewModel
            {
                Id = ph.Id,
                Name = ph.Name,
                Alt = ph.Alt,
                FilePath = ph.FilePath
            }).ToList(),
            Comments = post.Comments.OrderBy(c => c.CreationDate).Select(c => new CommentViewModel
            {
                Id = c.Id,
                Content = c.Content,
                CreationDate = c.CreationDate,
                EditDate = c.EditDate,
                AuthorName = $"{c.Author?.FirstName} {c.Author?.LastName}",
                AuthorId = c.AuthorId,
                Photos = c.Photos.Select(ph => new PhotoViewModel
                {
                    Id = ph.Id,
                    Name = ph.Name,
                    Alt = ph.Alt,
                    FilePath = ph.FilePath
                }).ToList()
            }).ToList()
        };

        ViewBag.TripId = post.Blog?.TripId;
        return View(viewModel);
    }

    // GET: Blog/CreatePost/5
    [HttpGet]
    public async Task<IActionResult> CreatePost(int blogId)
    {
        var currentUserId = _userManager.GetUserId(User);
        if (!await _postService.CanUserCreatePostAsync(blogId, currentUserId!))
        {
            return Forbid();
        }

        // Pobierz blog i trip
        var blog = await _blogService.GetByIdAsync(blogId);
        if (blog == null)
        {
            return NotFound();
        }

        // Pobierz dni z tripa
        var days = await _dayService.GetDaysByTripIdAsync(blog.TripId);
        var daySelectItems = days.Select(d => new DaySelectItem
        {
            Id = d.Id,
            Number = d.Number,
            Name = d.Name ?? string.Empty,
            TripId = d.TripId
        }).ToList();

        var viewModel = new CreatePostViewModel
        {
            BlogId = blogId,
            Days = daySelectItems
        };

        // Przekaż tripId do widoku dla przycisku Anuluj
        ViewBag.TripId = blog.TripId;

        return View(viewModel);
    }

    // POST: Blog/CreatePost/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePost(CreatePostViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var currentUserId = _userManager.GetUserId(User);
        if (!await _postService.CanUserCreatePostAsync(model.BlogId, currentUserId!))
        {
            return Forbid();
        }

        var post = new Post
        {
            Title = model.Title,
            Content = model.Content,
            BlogId = model.BlogId,
            DayId = model.DayId,
            AuthorId = currentUserId!,
            CreationDate = DateTime.UtcNow
        };

        var createdPost = await _postService.AddAsync(post);

        if (model.Photos != null && model.Photos.Count > 0)
        {
            await _photoService.AddMultipleBlogPhotosAsync(
                model.Photos,
                _webHostEnvironment.WebRootPath,
                "posts",
                postId: createdPost.Id
            );
        }

        return RedirectToAction("Post", new { id = createdPost.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateComment(CreateCommentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Comment cannot be empty";
            return RedirectToAction("Post", new { id = model.PostId });
        }

        var currentUserId = _userManager.GetUserId(User);
        if (!await _commentService.CanUserCreateCommentAsync(model.PostId, currentUserId!))
        {
            return Forbid();
        }

        var comment = new Comment
        {
            Content = model.Content,
            PostId = model.PostId,
            AuthorId = currentUserId!,
            CreationDate = DateTime.UtcNow
        };

        var createdComment = await _commentService.AddAsync(comment);

        if (model.Photos != null && model.Photos.Count > 0)
        {
            await _photoService.AddMultipleBlogPhotosAsync(
                model.Photos,
                _webHostEnvironment.WebRootPath,
                "comments",
                commentId: createdComment.Id
            );
        }

        TempData["Success"] = "Comment added successfully!";
        return RedirectToAction("Post", new { id = model.PostId });
    }

    [HttpGet]
    public async Task<IActionResult> EditPost(int id)
    {
        var post = await _postService.GetWithDetailsAsync(id);
        if (post == null)
        {
            return NotFound();
        }

        var currentUserId = _userManager.GetUserId(User);
        if (post.AuthorId != currentUserId)
        {
            return Forbid();
        }

        // Pobierz dni z tripa
        var days = await _dayService.GetDaysByTripIdAsync(post.Blog!.TripId);
        var daySelectItems = days.Select(d => new DaySelectItem
        {
            Id = d.Id,
            Number = d.Number,
            Name = d.Name ?? string.Empty,
            TripId = d.TripId
        }).ToList();

        var viewModel = new CreatePostViewModel
        {
            Title = post.Title,
            Content = post.Content,
            BlogId = post.BlogId,
            DayId = post.DayId,
            Days = daySelectItems
        };

        ViewBag.PostId = id;
        ViewBag.Photos = post.Photos.Select(p => new PhotoViewModel
        {
            Id = p.Id,
            Name = p.Name,
            Alt = p.Alt,
            FilePath = p.FilePath
        }).ToList();

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPost(int id, CreatePostViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var post = await _postService.GetByIdAsync(id);
        if (post == null)
        {
            return NotFound();
        }

        var currentUserId = _userManager.GetUserId(User);
        if (post.AuthorId != currentUserId)
        {
            return Forbid();
        }

        post.Title = model.Title;
        post.Content = model.Content;
        post.DayId = model.DayId;
        post.EditDate = DateTime.UtcNow;

        await _postService.UpdateAsync(post);

        if (model.Photos != null && model.Photos.Count > 0)
        {
            await _photoService.AddMultipleBlogPhotosAsync(
                model.Photos,
                _webHostEnvironment.WebRootPath,
                "posts",
                postId: id
            );
        }

        TempData["Success"] = "Post updated successfully!";
        return RedirectToAction("Post", new { id = id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePost(int id)
    {
        var post = await _postService.GetByIdAsync(id);
        if (post == null)
        {
            return NotFound();
        }

        var currentUserId = _userManager.GetUserId(User);
        if (post.AuthorId != currentUserId)
        {
            return Forbid();
        }

        var tripId = post.Blog?.TripId;
        await _postService.DeleteAsync(id);

        TempData["Success"] = "Post deleted successfully!";
        return RedirectToAction("Index", new { tripId = tripId });
    }

    // GET: Blog/EditComment/5
    [HttpGet]
    public async Task<IActionResult> EditComment(int id)
    {
        var comment = await _commentService.GetByIdAsync(id);
        if (comment == null)
        {
            return NotFound();
        }

        var currentUserId = _userManager.GetUserId(User);
        if (!await _commentService.CanUserEditCommentAsync(id, currentUserId!))
        {
            TempData["ErrorMessage"] = "You can only edit your own comments.";
            return RedirectToAction("Post", new { id = comment.PostId });
        }

        var viewModel = new EditCommentViewModel
        {
            Id = comment.Id,
            Content = comment.Content,
            PostId = comment.PostId,
            PostTitle = comment.Post?.Title ?? "Unknown Post",
            ExistingPhotos = comment.Photos.Select(p => new PhotoViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Alt = p.Alt,
                FilePath = p.FilePath
            }).ToList()
        };

        return View(viewModel);
    }

    // POST: Blog/EditComment/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditComment(int id, EditCommentViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existingComment = await _commentService.GetByIdAsync(id);
        if (existingComment == null)
        {
            return NotFound();
        }

        var currentUserId = _userManager.GetUserId(User);
        if (!await _commentService.CanUserEditCommentAsync(id, currentUserId!))
        {
            TempData["ErrorMessage"] = "You can only edit your own comments.";
            return RedirectToAction("Post", new { id = existingComment.PostId });
        }

        try
        {
            // Aktualizuj właściwości komentarza
            existingComment.Content = model.Content;
            existingComment.EditDate = DateTime.UtcNow;

            // Zaktualizuj komentarz z nowymi zdjęciami
            var webRootPath = _webHostEnvironment.WebRootPath;
            await _commentService.UpdateCommentAsync(existingComment, model.NewPhotos, webRootPath);

            TempData["SuccessMessage"] = "Comment updated successfully!";
            return RedirectToAction("Post", new { id = existingComment.PostId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating comment {CommentId}", id);
            ModelState.AddModelError("", "An error occurred while updating the comment.");
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComment(int id)
    {
        var comment = await _commentService.GetByIdAsync(id);
        if (comment == null)
        {
            return NotFound();
        }

        var currentUserId = _userManager.GetUserId(User);
        if (comment.AuthorId != currentUserId)
        {
            return Forbid();
        }

        var postId = comment.PostId;
        await _commentService.DeleteAsync(id);

        TempData["Success"] = "Comment deleted successfully!";
        return RedirectToAction("Post", new { id = postId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePostPhoto(int photoId, int postId)
    {
        var photo = await _photoService.GetByIdAsync(photoId);
        if (photo == null || photo.PostId != postId)
        {
            return NotFound();
        }

        var post = await _postService.GetByIdAsync(postId);
        var currentUserId = _userManager.GetUserId(User);

        if (post == null || post.AuthorId != currentUserId)
        {
            return Forbid();
        }

        var webRootPath = _webHostEnvironment.WebRootPath;
        await _photoService.DeletePhotoAsync(photoId, webRootPath);

        TempData["Success"] = "Photo deleted successfully!";
        return RedirectToAction("EditPost", new { id = postId });
    }

    // POST: Blog/DeleteCommentPhoto/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCommentPhoto(int photoId, int commentId)
    {
        var photo = await _photoService.GetByIdAsync(photoId);
        if (photo == null || photo.CommentId != commentId)
        {
            return NotFound();
        }

        var comment = await _commentService.GetByIdAsync(commentId);
        var currentUserId = _userManager.GetUserId(User);

        if (comment == null || comment.AuthorId != currentUserId)
        {
            return Forbid();
        }

        var webRootPath = _webHostEnvironment.WebRootPath;
        await _photoService.DeletePhotoAsync(photoId, webRootPath);

        TempData["SuccessMessage"] = "Photo deleted successfully!";
        return RedirectToAction("EditComment", new { id = commentId });
    }

    // GET: Blog/PublicBlogs
    [HttpGet]
    public async Task<IActionResult> PublicBlogs(string? countryCode, string? searchTerm)
    {
        var publicBlogs = await _blogService.GetPublicBlogsAsync();

        // Apply country filter
        if (!string.IsNullOrEmpty(countryCode))
        {
            publicBlogs = publicBlogs.Where(b => b.Countries.Any(c => c.Code == countryCode)).ToList();
        }

        // Apply search filter
        if (!string.IsNullOrEmpty(searchTerm))
        {
            var search = searchTerm.ToLower();
            publicBlogs = publicBlogs.Where(b =>
                b.Name.ToLower().Contains(search) ||
                b.Description?.ToLower().Contains(search) == true ||
                b.TripName.ToLower().Contains(search) ||
                b.Catalog?.ToLower().Contains(search) == true
            ).ToList();
        }

        var availableCountries = await _blogService.GetCountriesWithPublicBlogsAsync();

        var viewModel = new PublicBlogsViewModel
        {
            Blogs = publicBlogs.Select(b => new PublicBlogItemViewModel
            {
                Id = b.Id,
                Name = b.Name,
                Description = b.Description,
                Catalog = b.Catalog,
                TripName = b.TripName,
                TripId = b.TripId,
                PostsCount = b.PostsCount,
                CommentsCount = b.CommentsCount,
                Countries = b.Countries.Select(c => c.Name).ToList(),
                LatestPostId = b.LatestPostId,
                LatestPostDate = b.LatestPostDate
            }).ToList(),
            AvailableCountries = availableCountries.Select(c => new CountryViewModel
            {
                Code = c.Code,
                Name = c.Name,
                BlogCount = c.BlogCount
            }).ToList(),
            SelectedCountryCode = countryCode,
            SearchTerm = searchTerm,
            TotalBlogs = publicBlogs.Count,
            TotalPosts = publicBlogs.Sum(b => b.PostsCount),
            TotalComments = publicBlogs.Sum(b => b.CommentsCount),
            TotalCountries = availableCountries.Count
        };

        return View(viewModel);
    }

    // GET: Blog/PublicView/5
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> PublicView(int id)
    {
        var blog = await _blogService.GetWithPostsAsync(id);
        if (blog == null)
        {
            return NotFound();
        }

        // Sprawdź czy blog jest publiczny
        if (blog.IsPrivate)
        {
            return Forbid();
        }

        var viewModel = new PublicBlogViewModel
        {
            Id = blog.Id,
            Name = blog.Name,
            Description = blog.Description,
            Catalog = blog.Catalog,
            TripName = blog.Trip?.Name ?? string.Empty,
            TripId = blog.TripId,
            IsPrivate = blog.IsPrivate,
            Posts = blog.Posts.Select(p => new PublicPostViewModel
            {
                Id = p.Id,
                Title = p.Title,
                Content = p.Content,
                CreationDate = p.CreationDate,
                EditDate = p.EditDate,
                AuthorName = $"{p.Author?.FirstName} {p.Author?.LastName}",
                AuthorId = p.AuthorId,
                DayId = p.DayId,
                DayName = p.Day?.Name,
                Photos = p.Photos.Select(ph => new PhotoViewModel
                {
                    Id = ph.Id,
                    Name = ph.Name,
                    Alt = ph.Alt,
                    FilePath = ph.FilePath
                }).ToList(),
                Comments = p.Comments.OrderBy(c => c.CreationDate).Select(c => new PublicCommentViewModel
                {
                    Id = c.Id,
                    Content = c.Content,
                    CreationDate = c.CreationDate,
                    EditDate = c.EditDate,
                    AuthorName = $"{c.Author?.FirstName} {c.Author?.LastName}",
                    AuthorId = c.AuthorId,
                    Photos = c.Photos.Select(ph => new PhotoViewModel
                    {
                        Id = ph.Id,
                        Name = ph.Name,
                        Alt = ph.Alt,
                        FilePath = ph.FilePath
                    }).ToList()
                }).ToList()
            }).ToList()
        };

        return View(viewModel);
    }

    // GET: Blog/PublicPost/5
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> PublicPost(int id)
    {
        var post = await _postService.GetWithDetailsAsync(id);
        if (post == null)
        {
            return NotFound();
        }

        // Sprawdź czy blog jest publiczny
        if (post.Blog?.IsPrivate == true)
        {
            return Forbid();
        }

        var viewModel = new PublicPostViewModel
        {
            Id = post.Id,
            Title = post.Title,
            Content = post.Content,
            CreationDate = post.CreationDate,
            EditDate = post.EditDate,
            AuthorName = $"{post.Author?.FirstName} {post.Author?.LastName}",
            AuthorId = post.AuthorId,
            BlogId = post.BlogId,
            DayId = post.DayId,
            DayName = post.Day?.Name,
            Photos = post.Photos.Select(ph => new PhotoViewModel
            {
                Id = ph.Id,
                Name = ph.Name,
                Alt = ph.Alt,
                FilePath = ph.FilePath
            }).ToList(),
            Comments = post.Comments.Select(c => new PublicCommentViewModel
            {
                Id = c.Id,
                Content = c.Content,
                CreationDate = c.CreationDate,
                EditDate = c.EditDate,
                AuthorName = $"{c.Author?.FirstName} {c.Author?.LastName}",
                AuthorId = c.AuthorId,
                Photos = c.Photos.Select(ph => new PhotoViewModel
                {
                    Id = ph.Id,
                    Name = ph.Name,
                    Alt = ph.Alt,
                    FilePath = ph.FilePath
                }).ToList()
            }).ToList()
        };

        ViewBag.TripId = post.Blog?.TripId;
        ViewBag.BlogId = post.BlogId;
        ViewBag.BlogName = post.Blog?.Name;
        ViewBag.IsPublicView = true;

        return View(viewModel);
    }
}