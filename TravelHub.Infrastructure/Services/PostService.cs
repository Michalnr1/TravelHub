using Microsoft.AspNetCore.Http;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;

namespace TravelHub.Infrastructure.Services;

public class PostService : GenericService<Post>, IPostService
{
    private readonly IPostRepository _postRepository;
    private readonly IPhotoService _photoService;
    private readonly IBlogService _blogService;
    private readonly ITripParticipantService _tripParticipantService;

    public PostService(IPostRepository repository, IPhotoService photoService,
        IBlogService blogService, ITripParticipantService tripParticipantService) : base(repository)
    {
        _postRepository = repository;
        _photoService = photoService;
        _blogService = blogService;
        _tripParticipantService = tripParticipantService;
    }

    public async Task<Post?> GetWithDetailsAsync(int id)
    {
        return await _postRepository.GetByIdWithDetailsAsync(id);
    }

    public async Task<IReadOnlyList<Post>> GetByBlogIdAsync(int blogId)
    {
        return await _postRepository.GetByBlogIdAsync(blogId);
    }

    public async Task<Post> CreatePostAsync(Post post, IFormFileCollection? photos, string webRootPath)
    {
        post.CreationDate = DateTime.UtcNow;

        var createdPost = await _postRepository.AddAsync(post);

        if (photos != null && photos.Count > 0)
        {
            foreach (var photo in photos)
            {
                if (photo.Length > 0)
                {
                    var photoEntity = await _photoService.AddBlogPhotoAsync(
                        photo,
                        webRootPath,
                        "posts",
                        postId: createdPost.Id
                    );
                    // Zdjęcie jest już zapisane w bazie przez AddBlogPhotoAsync
                }
            }
        }

        return createdPost;
    }

    public async Task<int?> GetTripIdByPostIdAsync(int postId)
    {
        return await _postRepository.GetTripIdByPostIdAsync(postId);
    }

    public async Task<bool> CanUserCreatePostAsync(int blogId, string userId)
    {
        // Pobierz tripId z bloga
        var blog = await _blogService.GetByIdAsync(blogId);
        if (blog == null) return false;

        // Sprawdź czy użytkownik jest uczestnikiem tripa
        return await _tripParticipantService.IsUserParticipantAsync(blog.TripId, userId);
    }

    public async Task<bool> CanUserEditPostAsync(int postId, string userId)
    {
        var post = await _postRepository.GetByIdAsync(postId);
        return post?.AuthorId == userId;
    }
}
