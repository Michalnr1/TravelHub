using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TravelHub.Domain.DTOs;
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
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<PostService> _logger;

    public PostService(IPostRepository repository, IPhotoService photoService,
        IBlogService blogService, ITripParticipantService tripParticipantService,
        IBackgroundJobClient backgroundJobClient, ILogger<PostService> logger) : base(repository)
    {
        _postRepository = repository;
        _photoService = photoService;
        _blogService = blogService;
        _tripParticipantService = tripParticipantService;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    public async Task<Post?> GetWithDetailsAsync(int id)
    {
        return await _postRepository.GetByIdWithDetailsAsync(id);
    }

    public async Task<IReadOnlyList<Post>> GetByBlogIdAsync(int blogId)
    {
        return await _postRepository.GetByBlogIdAsync(blogId);
    }

    public async Task<Post> CreatePostAsync(Post post, IFormFileCollection? photos, string webRootPath, bool isScheduled = false, DateTimeOffset? scheduledFor = null)
    {
        Post? createdPost;

        if (isScheduled && scheduledFor.HasValue)
        {
            post.IsScheduled = true;
            post.ScheduledFor = scheduledFor.Value.UtcDateTime; // Konwertuj na UTC
            post.CreationDate = DateTime.UtcNow;

            // Zapisz post jako zaplanowany
            createdPost = await _postRepository.AddAsync(post);

            // Zaplanuj publikację
            var jobId = _backgroundJobClient.Schedule<IPostService>(
                service => service.PublishScheduledPostAsync(createdPost.Id),
                scheduledFor.Value);

            createdPost.HangfireJobId = jobId;
            await _postRepository.UpdateAsync(createdPost);
        }
        else
        {
            // Natychmiastowa publikacja
            post.IsScheduled = false;
            post.PublishedDate = DateTime.UtcNow;
            post.CreationDate = DateTime.UtcNow;

            createdPost = await _postRepository.AddAsync(post);
        }

        if (photos != null && photos.Count > 0)
        {
            await _photoService.AddMultipleBlogPhotosAsync(
                photos,
                webRootPath,
                "posts",
                postId: createdPost.Id
            );
        }

        return createdPost;
    }

    public async Task PublishScheduledPostAsync(int postId)
    {
        var post = await _postRepository.GetByIdWithDetailsAsync(postId);
        if (post == null || !post.IsScheduled)
        {
            return;
        }

        // Opublikuj post
        post.IsScheduled = false;
        post.PublishedDate = DateTime.UtcNow;
        post.HangfireJobId = null; // Job został wykonany

        await _postRepository.UpdateAsync(post);

        // Tutaj możesz dodać powiadomienia itp.
        _logger.LogInformation("Scheduled post {PostId} published at {PublishTime}",
            postId, DateTime.UtcNow);
    }

    public async Task<bool> UpdateScheduledPostAsync(int postId, UpdateScheduledPostDto updateDto, IFormFileCollection? photos, string webRootPath)
    {
        var existingPost = await _postRepository.GetByIdAsync(postId);
        if (existingPost == null || !existingPost.IsScheduled)
        {
            return false;
        }

        // Jeśli zmieniono czas publikacji, przeplanuj job
        if (existingPost.ScheduledFor != updateDto.ScheduledFor)
        {
            // Usuń stary job
            if (!string.IsNullOrEmpty(existingPost.HangfireJobId))
            {
                _backgroundJobClient.Delete(existingPost.HangfireJobId);
            }

            // Utwórz nowy job
            var newJobId = _backgroundJobClient.Schedule<IPostService>(
                service => service.PublishScheduledPostAsync(existingPost.Id),
                updateDto.ScheduledFor);

            existingPost.HangfireJobId = newJobId;
        }

        // Zaktualizuj post
        existingPost.Title = updateDto.Title;
        existingPost.Content = updateDto.Content;
        existingPost.DayId = updateDto.DayId;
        existingPost.ScheduledFor = updateDto.ScheduledFor;
        //existingPost.EditDate = DateTime.UtcNow;

        await _postRepository.UpdateAsync(existingPost);

        // Dodaj nowe zdjęcia jeśli są
        if (photos != null && photos.Count > 0)
        {
            await _photoService.AddMultipleBlogPhotosAsync(
                photos,
                webRootPath,
                "posts",
                postId: existingPost.Id
            );
        }

        return true;
    }

    public async Task<bool> CancelScheduledPostAsync(int postId)
    {
        var post = await _postRepository.GetByIdAsync(postId);
        if (post == null || !post.IsScheduled)
        {
            return false;
        }

        // Usuń job z Hangfire
        if (!string.IsNullOrEmpty(post.HangfireJobId))
        {
            _backgroundJobClient.Delete(post.HangfireJobId);
        }

        // Usuń post
        await _postRepository.DeleteAsync(post);
        return true;
    }

    public async Task<IReadOnlyList<Post>> GetScheduledPostsAsync(int blogId)
    {
        return await _postRepository.GetScheduledPostsByBlogIdAsync(blogId);
    }

    public async Task<IReadOnlyList<Post>> GetPublishedPostsAsync(int blogId)
    {
        return await _postRepository.GetPublishedPostsByBlogIdAsync(blogId);
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

    public async Task<int?> GetBlogIdByPostIdAsync(int postId)
    {
        return await _postRepository.GetBlogIdByPostIdAsync(postId);
    }
}
