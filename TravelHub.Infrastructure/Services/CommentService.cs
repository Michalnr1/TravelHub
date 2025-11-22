using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;

namespace TravelHub.Infrastructure.Services;

public class CommentService : GenericService<Comment>, ICommentService
{
    private readonly ICommentRepository _commentRepository;
    private readonly IPhotoService _photoService;
    private readonly ITripParticipantService _tripParticipantService;
    private readonly IPostService _postService;
    private readonly ILogger<CommentService> _logger;

    public CommentService(ICommentRepository repository,
        IPhotoService photoService,
        ITripParticipantService tripParticipantService,
        IPostService postService,
        ILogger<CommentService> logger) : base(repository)
    {
        _commentRepository = repository;
        _photoService = photoService;
        _tripParticipantService = tripParticipantService;
        _postService = postService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Comment>> GetByPostIdAsync(int postId)
    {
        return await _commentRepository.GetCommentsByPostIdAsync(postId);
    }

    public async Task<Comment> CreateCommentAsync(Comment comment, IFormFileCollection? photos, string webRootPath)
    {
        comment.CreationDate = DateTime.UtcNow;

        var createdComment = await _commentRepository.AddAsync(comment);

        if (photos != null && photos.Count > 0)
        {
            foreach (var photo in photos)
            {
                if (photo.Length > 0)
                {
                    var photoEntity = await _photoService.AddBlogPhotoAsync(
                        photo,
                        webRootPath,
                        "comments",
                        commentId: createdComment.Id
                    );
                    // Zdjęcie jest już zapisane w bazie przez AddBlogPhotoAsync
                }
            }
        }

        return createdComment;
    }

    public async Task<bool> CanUserCreateCommentAsync(int postId, string userId)
    {
        try
        {
            // Pobierz tripId z posta
            var tripId = await _postService.GetTripIdByPostIdAsync(postId);
            if (!tripId.HasValue) return false;

            // Sprawdź czy użytkownik jest uczestnikiem tripa
            return await _tripParticipantService.IsUserParticipantAsync(tripId.Value, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user can create comment for post {PostId}", postId);
            return false;
        }
    }

    public async Task<Comment> UpdateCommentAsync(Comment comment, IFormFileCollection? newPhotos, string webRootPath)
    {
        comment.EditDate = DateTime.UtcNow;

        await _commentRepository.UpdateAsync(comment);

        // Dodaj nowe zdjęcia jeśli są
        if (newPhotos != null && newPhotos.Count > 0)
        {
            foreach (var photo in newPhotos)
            {
                if (photo.Length > 0)
                {
                    var photoEntity = await _photoService.AddBlogPhotoAsync(
                        photo,
                        webRootPath,
                        "comments",
                        commentId: comment.Id
                    );
                }
            }
        }

        return comment;
    }

    public async Task<bool> CanUserEditCommentAsync(int commentId, string userId)
    {
        var comment = await _commentRepository.GetByIdAsync(commentId);
        return comment?.AuthorId == userId;
    }
}