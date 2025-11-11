using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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

    // --- new method: add encrypted file and return generated password ---
    /// <summary>
    /// Saves and encrypts the incoming stream to disk using AES-GCM.
    /// Returns tuple: created File entity and generated password (plain) - pass the password securely to user.
    /// </summary>
    public async Task<(Domain.Entities.File CreatedFile, string Password)> AddEncryptedFileAsync(
        Domain.Entities.File file,
        Stream fileStream,
        string originalFileName,
        string webRootPath)
    {
        if (fileStream == null) throw new ArgumentNullException(nameof(fileStream));
        if (string.IsNullOrWhiteSpace(originalFileName)) throw new ArgumentException("File Name is required", nameof(originalFileName));

        var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new ArgumentException("Not allowed file format");

        // size check
        try
        {
            if (fileStream.CanSeek && fileStream.Length > MaxFileBytes)
                throw new ArgumentException($"File is too big. Maximum {MaxFileBytes / (1024 * 1024)} MB.");
        }
        catch { }

        // generate strong password for this file
        var password = TravelHub.Infrastructure.Security.CryptoUtils.GenerateStrongPassword();

        // generate salt for PBKDF2
        var salt = new byte[16];
        RandomNumberGenerator.Fill(salt);

        // derive key
        var key = TravelHub.Infrastructure.Security.CryptoUtils.DeriveKeyFromPassword(password, salt);

        // generate nonce (IV) for AES-GCM (12 bytes recommended)
        var nonce = new byte[12];
        RandomNumberGenerator.Fill(nonce);

        var uploadsFolder = Path.Combine(webRootPath, "files", "spots");
        Directory.CreateDirectory(uploadsFolder);

        var safeFileName = $"{Guid.NewGuid()}{ext}.enc"; // store encrypted bytes with .enc appended
        var filePath = Path.Combine(uploadsFolder, safeFileName);

        // Encrypt: AES-GCM
        // We'll write to disk: [ciphertext][tag] and keep nonce and salt in DB
        using (var outFs = System.IO.File.Create(filePath))
        {
            // read input fully into memory? better to stream and encrypt chunkwise with AesGcm - but AesGcm works on whole arrays.
            // For simplicity and typical pdf sizes (<5MB), buffer in memory
            using var ms = new MemoryStream();
            await fileStream.CopyToAsync(ms);
            var plaintext = ms.ToArray();

            var cipher = new byte[plaintext.Length];
            var tag = new byte[16];

            using (var aesGcm = new AesGcm(key))
            {
                aesGcm.Encrypt(nonce, plaintext, cipher, tag, null);
            }

            // Write: nonce (12) | cipher | tag (16)
            // But we already store nonce in DB; we can still write ciphertext+tag only.
            // To simplify reading, we'll write ciphertext then tag.
            await outFs.WriteAsync(cipher, 0, cipher.Length);
            await outFs.WriteAsync(tag, 0, tag.Length);
        }

        // Fill file metadata and save DB record
        file.Name = safeFileName;                       // stored file name on disk
        file.DisplayName = originalFileName;            // original name for user
        file.IsEncrypted = true;
        file.SaltBase64 = Convert.ToBase64String(salt);
        file.NonceBase64 = Convert.ToBase64String(nonce);

        var created = await _fileRepository.AddAsync(file);

        // Return created entity and the password (plain) - handle password securely in caller
        return (created, password);
    }

    /// <summary>
    /// Decrypts the stored encrypted file with the provided password and returns a MemoryStream containing plaintext bytes.
    /// Throws if password is wrong or file missing.
    /// </summary>
    public async Task<Stream> DecryptFileToStreamAsync(int fileId, string password, string webRootPath)
    {
        var fileMeta = await _fileRepository.GetByIdAsync(fileId);
        if (fileMeta == null) throw new FileNotFoundException("File not found.");

        if (!fileMeta.IsEncrypted || string.IsNullOrEmpty(fileMeta.SaltBase64) || string.IsNullOrEmpty(fileMeta.NonceBase64))
            throw new InvalidOperationException("File is not encrypted or missing encryption metadata.");

        var salt = Convert.FromBase64String(fileMeta.SaltBase64);
        var nonce = Convert.FromBase64String(fileMeta.NonceBase64);
        var key = TravelHub.Infrastructure.Security.CryptoUtils.DeriveKeyFromPassword(password, salt);

        var uploadsFolder = Path.Combine(webRootPath, "files", "spots");
        var filePath = Path.Combine(uploadsFolder, fileMeta.Name ?? "");

        if (!System.IO.File.Exists(filePath)) throw new FileNotFoundException("Encrypted file missing on disk.");

        byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);

        if (fileBytes.Length < 16) throw new InvalidDataException("Invalid encrypted file.");

        // last 16 bytes are tag, rest are ciphertext
        var tag = fileBytes.Skip(fileBytes.Length - 16).Take(16).ToArray();
        var cipher = fileBytes.Take(fileBytes.Length - 16).ToArray();

        var plaintext = new byte[cipher.Length];

        try
        {
            using var aesGcm = new AesGcm(key);
            aesGcm.Decrypt(nonce, cipher, tag, plaintext, null);
        }
        catch (CryptographicException)
        {
            throw new UnauthorizedAccessException("Invalid password or corrupted file.");
        }

        return new MemoryStream(plaintext); // caller should dispose
    }
}