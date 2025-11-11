using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;

namespace TravelHub.Infrastructure.Services;

public class FileService : GenericService<Domain.Entities.File>, IFileService
{
    private readonly IFileRepository _fileRepository;
    private static readonly string[] AllowedExtensions = new[] { ".pdf" };
    private const long MaxFileBytes = 5 * 1024 * 1024; // 5 MB

    public FileService(IFileRepository fileRepository) : base(fileRepository)
    {
        _fileRepository = fileRepository;
    }

    public async Task<IReadOnlyList<Domain.Entities.File>> GetBySpotIdAsync(int spotId)
    {
        return await _fileRepository.GetFilesBySpotIdAsync(spotId);
    }

    public async Task<Domain.Entities.File> AddFileAsync(Domain.Entities.File file, Stream fileStream, string fileName, string webRootPath)
    {
        if (fileStream == null) throw new ArgumentNullException(nameof(fileStream));
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("File Name is required", nameof(fileName));

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new ArgumentException("Not allowed file format");

        // if stream has length and is too big, reject
        try
        {
            if (fileStream.CanSeek && fileStream.Length > MaxFileBytes)
                throw new ArgumentException($"File is too big. Maximum {MaxFileBytes / (1024 * 1024)} MB.");
        }
        catch
        {
            // if cannot read Length, skip the check
        }

        var uploadsFolder = Path.Combine(webRootPath, "files", "spots");
        Directory.CreateDirectory(uploadsFolder);

        var safeFileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadsFolder, safeFileName);

        // save stream to file
        using (var fs = System.IO.File.Create(filePath))
        {
            await fileStream.CopyToAsync(fs);
        }

        file.Name = safeFileName;
        var created = await _fileRepository.AddAsync(file);
        return created;
    }

    public async Task DeleteFileAsync(int id, string webRootPath)
    {
        var file = await _fileRepository.GetByIdAsync(id);
        if (file == null) return;

        var filePath = Path.Combine(webRootPath, "files", "spots", file.Name ?? "");
        if (System.IO.File.Exists(filePath))
        {
            try { System.IO.File.Delete(filePath); } catch { /* log if necessary */ }
        }

        await _fileRepository.DeleteAsync(file);
    }
}