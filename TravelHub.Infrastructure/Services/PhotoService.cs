using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;

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

    public async Task<IReadOnlyList<Photo>> GetBySpotIdAsync(int spotId)
    {
        return await _photoRepository.GetPhotosBySpotIdAsync(spotId);
    }

    public async Task<Photo> AddPhotoAsync(Photo photo, Stream fileStream, string fileName, string webRootPath)
    {
        if (fileStream == null) throw new ArgumentNullException(nameof(fileStream));
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("Nazwa pliku jest wymagana.", nameof(fileName));

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new ArgumentException("Nieobsługiwany format pliku.");

        // Jeśli stream ma długość i jest za duży, odrzuć
        try
        {
            if (fileStream.CanSeek && fileStream.Length > MaxFileBytes)
                throw new ArgumentException($"Plik jest za duży. Maksymalnie {MaxFileBytes / (1024 * 1024)} MB.");
        }
        catch
        {
            // jeśli nie można odczytać Length, pomijamy sprawdzenie (ale można dodać dodatkowe zabezpieczenia)
        }

        var uploadsFolder = Path.Combine(webRootPath, "images", "spots");
        Directory.CreateDirectory(uploadsFolder);

        var safeFileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadsFolder, safeFileName);

        // Zapis strumienia do pliku
        using (var fs = File.Create(filePath))
        {
            await fileStream.CopyToAsync(fs);
        }

        photo.Name = safeFileName;
        if (photo.Alt == null) { photo.Alt = "no alternative image description"; }
        var created = await _photoRepository.AddAsync(photo);
        return created;
    }

    public async Task DeletePhotoAsync(int id, string webRootPath)
    {
        var photo = await _photoRepository.GetByIdAsync(id);
        if (photo == null) return;

        var filePath = Path.Combine(webRootPath, "images", "spots", photo.Name ?? "");
        if (File.Exists(filePath))
        {
            try { File.Delete(filePath); } catch { /* log if necessary */ }
        }

        await _photoRepository.DeleteAsync(photo);
    }
}