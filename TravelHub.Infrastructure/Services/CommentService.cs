using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Infrastructure.Email;

namespace TravelHub.Infrastructure.Services;

public class CommentService : GenericService<Comment>, ICommentService
{
    private readonly ICommentRepository _commentRepository;
    private readonly IPhotoService _photoService;
    private readonly ITripParticipantService _tripParticipantService;
    private readonly IPostService _postService;
    private readonly IBlogService _blogService;
    private readonly IFriendshipService _friendshipService;
    private readonly UserManager<Person> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<CommentService> _logger;
    private readonly string _websiteUrl;

    public CommentService(ICommentRepository repository,
        IPhotoService photoService,
        ITripParticipantService tripParticipantService,
        IPostService postService,
        IBlogService blogService,
        IFriendshipService friendshipService,
        UserManager<Person> userManager,
        IEmailSender emailSender,
        IOptions<EmailSettings> emailSettings,
        ILogger<CommentService> logger) : base(repository)
    {
        _commentRepository = repository;
        _photoService = photoService;
        _tripParticipantService = tripParticipantService;
        _postService = postService;
        _blogService = blogService;
        _friendshipService = friendshipService;
        _userManager = userManager;
        _emailSender = emailSender;
        _websiteUrl = emailSettings.Value.WebsiteUrl;
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

        // Wyślij powiadomienie email
        await SendCommentNotificationEmailAsync(createdComment);

        return createdComment;
    }

    public async Task<bool> CanUserCreateCommentAsync(int postId, string userId)
    {
        try
        {
            // Pobierz blogId z posta
            var blogId = await _postService.GetBlogIdByPostIdAsync(postId);
            if (!blogId.HasValue) return false;

            // Sprawdź czy użytkownik może komentować na blogu
            return await _blogService.CanUserCommentOnBlogAsync(blogId.Value, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user can create comment for post {PostId}", postId);
            return false;
        }
    }

    public async Task<bool> CanUserAccessCommentsAsync(int postId, string userId)
    {
        try
        {
            // Pobierz blogId z posta
            var blogId = await _postService.GetBlogIdByPostIdAsync(postId);
            if (!blogId.HasValue) return false;

            // Sprawdź czy użytkownik ma dostęp do bloga
            return await _blogService.CanUserAccessBlogAsync(blogId.Value, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user can access comments for post {PostId}", postId);
            return false;
        }
    }

    public async Task<Comment> UpdateCommentAsync(Comment comment, IFormFileCollection? newPhotos, string webRootPath)
    {
        comment.EditDate = DateTime.UtcNow;

        await _commentRepository.UpdateAsync(comment);

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

    private async Task SendCommentNotificationEmailAsync(Comment comment)
    {
        try
        {
            // Pobierz szczegóły posta
            var post = await _postService.GetWithDetailsAsync(comment.PostId);
            if (post == null) return;

            // Pobierz autora posta
            var postAuthor = post.Author;
            if (postAuthor == null || postAuthor.Id == comment.AuthorId) return; // Don't send if comment author is post author

            // Sprawdź czy użytkownik chce otrzymywać powiadomienia
            if (!postAuthor.ReceiveCommentNotifications)
            {
                _logger.LogInformation("User {UserId} has disabled comment notifications", postAuthor.Id);
                return;
            }

            // Pobierz autora komentarza
            var commentAuthor = await _userManager.FindByIdAsync(comment.AuthorId);
            var commentAuthorName = commentAuthor?.UserName ?? "User";

            // Przygotuj treść emaila
            var subject = $"Your post has been commented by {commentAuthorName}";
            var htmlMessage = GenerateCommentEmailHtml(post.Title, commentAuthorName, comment.Content, comment.CreationDate, comment.PostId);

            // Wyślij email
            await _emailSender.SendEmailAsync(postAuthor.Email!, subject, htmlMessage);

            _logger.LogInformation("Comment notification email sent to {Email} for post {PostId}",
                postAuthor.Email, comment.PostId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send comment notification email for comment {CommentId}", comment.Id);
            // Don't throw exception - email sending error shouldn't interrupt commenting process
        }
    }

    private string GenerateCommentEmailHtml(string postTitle, string commentAuthorName, string commentContent, DateTime commentDate, int postId)
    {
        var postUrl = $"{_websiteUrl}/Blog/Post/{postId}";
        var settingsUrl = $"{_websiteUrl}/Identity/Account/Manage";

        return $@"
        <!DOCTYPE html>
        <html lang=""en"">
        <head>
            <meta charset=""UTF-8"">
            <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
            <title>New Comment</title>
            <style>
                body {{
                    font-family: 'Arial', sans-serif;
                    line-height: 1.6;
                    color: #333;
                    margin: 0;
                    padding: 0;
                    background-color: #f4f4f4;
                }}
                .container {{
                    max-width: 600px;
                    margin: 0 auto;
                    background: #ffffff;
                    border-radius: 10px;
                    overflow: hidden;
                    box-shadow: 0 4px 6px rgba(0,0,0,0.1);
                }}
                .header {{
                    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                    color: white;
                    padding: 30px 20px;
                    text-align: center;
                }}
                .header h1 {{
                    margin: 0;
                    font-size: 24px;
                    font-weight: bold;
                }}
                .content {{
                    padding: 30px;
                }}
                .post-title {{
                    font-size: 20px;
                    font-weight: bold;
                    color: #2c3e50;
                    margin-bottom: 20px;
                    text-align: center;
                }}
                .comment-info {{
                    background: #f8f9fa;
                    padding: 20px;
                    border-radius: 8px;
                    margin-bottom: 20px;
                    border-left: 4px solid #667eea;
                }}
                .comment-author {{
                    font-weight: bold;
                    color: #667eea;
                    font-size: 16px;
                }}
                .comment-date {{
                    color: #6c757d;
                    font-size: 14px;
                    margin-bottom: 15px;
                }}
                .comment-content {{
                    background: white;
                    padding: 15px;
                    border-radius: 8px;
                    border: 1px solid #e9ecef;
                    font-style: italic;
                }}
                .button {{
                    display: inline-block;
                    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                    color: white;
                    padding: 12px 30px;
                    text-decoration: none;
                    border-radius: 25px;
                    font-weight: bold;
                    margin: 20px 0;
                    text-align: center;
                }}
                .footer {{
                    text-align: center;
                    padding: 20px;
                    background: #f8f9fa;
                    color: #6c757d;
                    font-size: 14px;
                }}
                .social-links {{
                    margin: 20px 0;
                }}
                .social-links a {{
                    margin: 0 10px;
                    color: #667eea;
                    text-decoration: none;
                }}
            </style>
        </head>
        <body>
            <div class=""container"">
                <div class=""header"">
                    <h1>🚀 TravelHub</h1>
                    <p>New comment under your post</p>
                </div>
        
                <div class=""content"">
                    <div class=""post-title"">
                        📝 {WebUtility.HtmlEncode(postTitle)}
                    </div>
            
                    <div class=""comment-info"">
                        <div class=""comment-author"">
                            👤 {WebUtility.HtmlEncode(commentAuthorName)} added a comment:
                        </div>
                        <div class=""comment-date"">
                            📅 {commentDate:dd.MM.yyyy HH:mm}
                        </div>
                        <div class=""comment-content"">
                            {FormatCommentContent(commentContent)}
                        </div>
                    </div>
            
                    <div style=""text-align: center;"">
                        <a href=""{postUrl}"" class=""button"">
                            View Comment
                        </a
                    </div>
            
                    <p style=""text-align: center; color: #6c757d;"">
                        Click the button above to go to the post and see the full discussion.
                    </p>
                </div>
        
                <div class=""footer"">
                    <p>© {DateTime.Now.Year} TravelHub. All rights reserved.</p>
                    <p>
                        This is an automated message. Please do not reply to it.<br>
                        If you don't want to receive such notifications, 
                        <a href=""{settingsUrl}"" style=""color: #667eea;"">
                            change notification settings
                        </a>
                    </p>
                </div>
            </div>
        </body>
        </html>";
    }

    private string FormatCommentContent(string commentContent)
    {
        if (string.IsNullOrEmpty(commentContent))
            return string.Empty;

        // Zamień tagi HTML na zwykły tekst, zachowując podstawowe formatowanie
        var formattedContent = commentContent
            .Replace("<br>", "\n")
            .Replace("<br/>", "\n")
            .Replace("<br />", "\n")
            .Replace("<p>", "")
            .Replace("</p>", "\n\n")
            .Replace("<strong>", "**")
            .Replace("</strong>", "**")
            .Replace("<em>", "*")
            .Replace("</em>", "*");

        // Usuń pozostałe tagi HTML
        formattedContent = System.Text.RegularExpressions.Regex.Replace(
            formattedContent,
            "<.*?>",
            string.Empty
        );

        // Zakoduj pozostałe znaki specjalne HTML
        formattedContent = WebUtility.HtmlEncode(formattedContent);

        // Zamień znaki nowej linii na <br> dla poprawnego wyświetlania w HTML
        formattedContent = formattedContent
            .Replace("\n\n", "<br><br>")
            .Replace("\n", "<br>");

        return formattedContent;
    }
}