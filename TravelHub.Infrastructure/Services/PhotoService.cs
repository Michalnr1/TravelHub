using Microsoft.AspNetCore.Http;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;
using File = System.IO.File;

namespace TravelHub.Infrastructure.Services;

public class PhotoService : GenericService<Photo>, IPhotoService
{
    private readonly IPhotoRepository _photoRepository;
    private static readonly string[] AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxFileBytes = 5 * 1024 * 1024; // 5 MB

    public PhotoService(IPhotoRepository photoRepository) : base(photoRepository)
    {
        _photoRepository = photoRepository;
    }

    // Spot methods
    public async Task<IReadOnlyList<Photo>> GetBySpotIdAsync(int spotId)
    {
        return await _photoRepository.GetPhotosBySpotIdAsync(spotId);
    }

    public async Task<Photo> AddSpotPhotoAsync(Photo photo, Stream fileStream, string fileName, string webRootPath)
    {
        if (fileStream == null) throw new ArgumentNullException(nameof(fileStream));
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("File name is required", nameof(fileName));

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new ArgumentException("Not allowed file format");

        try
        {
            if (fileStream.CanSeek && fileStream.Length > MaxFileBytes)
                throw new ArgumentException($"File is too big. Maximum {MaxFileBytes / (1024 * 1024)} MB.");
        }
        catch
        {
            // jeśli nie można odczytać Length, pomijamy sprawdzenie
        }

        var uploadsFolder = Path.Combine(webRootPath, "images", "spots");
        Directory.CreateDirectory(uploadsFolder);

        var safeFileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadsFolder, safeFileName);

        using (var fs = File.Create(filePath))
        {
            await fileStream.CopyToAsync(fs);
        }

        photo.Name = safeFileName;
        photo.FilePath = $"/images/spots/{safeFileName}";
        if (string.IsNullOrEmpty(photo.Alt))
        {
            photo.Alt = "no alternative image description";
        }

        var created = await _photoRepository.AddAsync(photo);
        return created;
    }

    public async Task DeleteSpotPhotoAsync(int id, string webRootPath)
    {
        var photo = await _photoRepository.GetByIdAsync(id);
        if (photo == null) return;

        if (!string.IsNullOrEmpty(photo.FilePath))
        {
            var fullPath = Path.Combine(webRootPath, photo.FilePath.TrimStart('/'));
            if (File.Exists(fullPath))
            {
                try { File.Delete(fullPath); } catch { /* log if necessary */ }
            }
        }

        await _photoRepository.DeleteAsync(photo);
    }

    // Blog methods
    public async Task<IReadOnlyList<Photo>> GetByPostIdAsync(int postId)
    {
        return await _photoRepository.GetPhotosByPostIdAsync(postId);
    }

    public async Task<IReadOnlyList<Photo>> GetByCommentIdAsync(int commentId)
    {
        return await _photoRepository.GetByCommentIdAsync(commentId);
    }

    public async Task<Photo> AddBlogPhotoAsync(IFormFile photoFile, string webRootPath, string subFolder, int? postId = null, int? commentId = null, string? altText = null)
    {
        if (photoFile == null || photoFile.Length == 0)
            throw new ArgumentException("No file to upload");

        var ext = Path.GetExtension(photoFile.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new ArgumentException("Not allowed file format");

        if (photoFile.Length > MaxFileBytes)
            throw new ArgumentException($"File is too big. Maximum {MaxFileBytes / (1024 * 1024)} MB.");

        var uploadsFolder = Path.Combine(webRootPath, "images", subFolder);
        Directory.CreateDirectory(uploadsFolder);

        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(photoFile.FileName)}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);
        var relativePath = $"/images/{subFolder}/{uniqueFileName}";

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await photoFile.CopyToAsync(fileStream);
        }

        var photo = new Photo
        {
            Name = Path.GetFileNameWithoutExtension(photoFile.FileName),
            Alt = altText ?? Path.GetFileNameWithoutExtension(photoFile.FileName),
            FilePath = relativePath,
            PostId = postId,
            CommentId = commentId
        };

        return await _photoRepository.AddAsync(photo);
    }

    public async Task<IEnumerable<Photo>> AddMultipleBlogPhotosAsync(IFormFileCollection photoFiles, string webRootPath, string subFolder, int? postId = null, int? commentId = null)
    {
        var photos = new List<Photo>();

        foreach (var photoFile in photoFiles)
        {
            if (photoFile.Length > 0)
            {
                try
                {
                    var photo = await AddBlogPhotoAsync(photoFile, webRootPath, subFolder, postId, commentId);
                    photos.Add(photo);
                }
                catch (Exception ex)
                {
                    // Log error but continue with other files
                    Console.WriteLine($"Error uploading photo {photoFile.FileName}: {ex.Message}");
                }
            }
        }

        return photos;
    }

    public async Task DeletePhotoAsync(int photoId, string webRootPath)
    {
        var photo = await _photoRepository.GetByIdAsync(photoId);
        if (photo == null) return;

        if (!string.IsNullOrEmpty(photo.FilePath))
        {
            var fullPath = Path.Combine(webRootPath, photo.FilePath.TrimStart('/'));
            if (File.Exists(fullPath))
            {
                try
                {
                    File.Delete(fullPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting photo file: {ex.Message}");
                }
            }
        }

        await _photoRepository.DeleteAsync(photo);
    }

    public async Task DeletePhotoFileAsync(string filePath, string webRootPath)
    {
        var fullPath = Path.Combine(webRootPath, filePath.TrimStart('/'));
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        await Task.CompletedTask;
    }

    public async Task<string> SavePhotoAsync(IFormFile photo, string webRootPath, string subFolder)
    {
        if (photo == null || photo.Length == 0)
            throw new ArgumentException("No file to upload");

        var ext = Path.GetExtension(photo.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new ArgumentException("Not allowed file format");

        if (photo.Length > MaxFileBytes)
            throw new ArgumentException($"File is too big. Maximum {MaxFileBytes / (1024 * 1024)} MB.");

        var uploadsFolder = Path.Combine(webRootPath, "images", subFolder);
        Directory.CreateDirectory(uploadsFolder);

        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(photo.FileName)}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await photo.CopyToAsync(fileStream);
        }

        return $"/images/{subFolder}/{uniqueFileName}";
    }
}