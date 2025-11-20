using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Services;

namespace TravelHub.Web.Controllers;

[Authorize]
public class PhotosController : Controller
{
    private readonly IPhotoService _photoService;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<PhotosController> _logger;

    public PhotosController(IPhotoService photoService, IWebHostEnvironment env, ILogger<PhotosController> logger)
    {
        _photoService = photoService;
        _env = env;
        _logger = logger;
    }

    // Spot Photos
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadSpotPhoto(int spotId, IFormFile photoFile, string? alt)
    {
        if (photoFile == null || photoFile.Length == 0)
        {
            TempData["ErrorMessage"] = "No file to upload";
            return RedirectToAction("Details", "Spots", new { id = spotId });
        }

        try
        {
            var photo = new Photo
            {
                SpotId = spotId,
                Alt = alt ?? Path.GetFileNameWithoutExtension(photoFile.FileName),
                Name = Path.GetFileNameWithoutExtension(photoFile.FileName),
                FilePath = string.Empty // Will be set in the service
            };

            using (var stream = photoFile.OpenReadStream())
            {
                await _photoService.AddSpotPhotoAsync(photo, stream, photoFile.FileName, _env.WebRootPath);
            }
            TempData["SuccessMessage"] = "Photo added";
        }
        catch (ArgumentException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading spot photo");
            TempData["ErrorMessage"] = "There was an error during photo upload";
        }

        return RedirectToAction("Details", "Spots", new { id = spotId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSpotPhoto(int id, int spotId)
    {
        await _photoService.DeleteSpotPhotoAsync(id, _env.WebRootPath);
        TempData["SuccessMessage"] = "Photo deleted";
        return RedirectToAction("Details", "Spots", new { id = spotId });
    }

    // Blog Photos - Post Photos
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPostPhoto(int postId, IFormFile photoFile, string? alt)
    {
        if (photoFile == null || photoFile.Length == 0)
        {
            TempData["ErrorMessage"] = "No file to upload";
            return RedirectToAction("Post", "Blog", new { id = postId });
        }

        try
        {
            await _photoService.AddBlogPhotoAsync(
                photoFile,
                _env.WebRootPath,
                "posts",
                postId: postId,
                altText: alt
            );
            TempData["SuccessMessage"] = "Photo added to post";
        }
        catch (ArgumentException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading post photo");
            TempData["ErrorMessage"] = "There was an error during photo upload";
        }

        return RedirectToAction("Post", "Blog", new { id = postId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePostPhoto(int id, int postId)
    {
        await _photoService.DeletePhotoAsync(id, _env.WebRootPath);
        TempData["SuccessMessage"] = "Photo deleted";
        return RedirectToAction("Post", "Blog", new { id = postId });
    }

    // Blog Photos - Comment Photos
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadCommentPhoto(int commentId, IFormFile photoFile, string? alt)
    {
        if (photoFile == null || photoFile.Length == 0)
        {
            TempData["ErrorMessage"] = "No file to upload";
            return RedirectToAction("Post", "Blog", new { id = GetPostIdFromComment(commentId) });
        }

        try
        {
            await _photoService.AddBlogPhotoAsync(
                photoFile,
                _env.WebRootPath,
                "comments",
                commentId: commentId,
                altText: alt
            );
            TempData["SuccessMessage"] = "Photo added to comment";
        }
        catch (ArgumentException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading comment photo");
            TempData["ErrorMessage"] = "There was an error during photo upload";
        }

        return RedirectToAction("Post", "Blog", new { id = GetPostIdFromComment(commentId) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCommentPhoto(int id, int commentId)
    {
        var postId = GetPostIdFromComment(commentId);
        await _photoService.DeletePhotoAsync(id, _env.WebRootPath);
        TempData["SuccessMessage"] = "Photo deleted";
        return RedirectToAction("Post", "Blog", new { id = postId });
    }

    // Helper method to get postId from commentId - potrzebujesz zaimplementować tę logikę
    private int GetPostIdFromComment(int commentId)
    {
        // TODO: Zaimplementuj pobieranie postId na podstawie commentId
        // Możesz to zrobić przez serwis komentarzy
        // Na razie zwracamy 0 - trzeba to poprawić
        return 0;
    }
}