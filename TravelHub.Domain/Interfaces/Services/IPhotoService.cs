using Microsoft.AspNetCore.Http;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Services;

public interface IPhotoService : IGenericService<Photo>
{
    // Spot methods
    Task<IReadOnlyList<Photo>> GetBySpotIdAsync(int spotId);
    Task<Photo> AddSpotPhotoAsync(Photo photo, Stream fileStream, string fileName, string webRootPath);
    Task DeleteSpotPhotoAsync(int id, string webRootPath);

    // Blog methods
    Task<IReadOnlyList<Photo>> GetByPostIdAsync(int postId);
    Task<IReadOnlyList<Photo>> GetByCommentIdAsync(int commentId);
    Task<Photo> AddBlogPhotoAsync(IFormFile photoFile, string webRootPath, string subFolder, int? postId = null, int? commentId = null, string? altText = null);
    Task<IEnumerable<Photo>> AddMultipleBlogPhotosAsync(IFormFileCollection photoFiles, string webRootPath, string subFolder, int? postId = null, int? commentId = null);
    Task DeletePhotoAsync(int photoId, string webRootPath);
    Task DeletePhotoFileAsync(string filePath, string webRootPath);
    Task<string> SavePhotoAsync(IFormFile photo, string webRootPath, string subFolder);
}