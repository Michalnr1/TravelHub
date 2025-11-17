using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Infrastructure.Services;
using TravelHub.Web.ViewModels.Security;

namespace TravelHub.Web.Controllers;

[Authorize]
public class FilesController : Controller
{
    private readonly IFileService _fileService;
    private readonly IWebHostEnvironment _env;

    public FilesController(IFileService fileService, IWebHostEnvironment env)
    {
        _fileService = fileService;
        _env = env;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(int spotId, IFormFile fileFile)
    {
        if (fileFile == null || fileFile.Length == 0)
        {
            TempData["ErrorMessage"] = "No file to upload";
            return RedirectToAction("Details", "Spots", new { id = spotId });
        }

        var file = new Domain.Entities.File { Name = fileFile.FileName, DisplayName = fileFile.FileName, SpotId = spotId };
        try
        {
            using (var stream = fileFile.OpenReadStream())
            {
                // Use encrypted upload: returns created entity and password
                var (created, password) = await ((FileService)_fileService).AddEncryptedFileAsync(file, stream, fileFile.FileName, _env.WebRootPath);
                TempData["SuccessMessage"] = "File added (encrypted).";
                // Show generated password to user (display once). maybe sending via secure channel instead?
                TempData["FilePassword"] = password;
            }
        }
        catch (ArgumentException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "There was an error during file upload";
        }

        return RedirectToAction("Details", "Spots", new { id = spotId });
    }

    // GET: show password form if encrypted and no password provided
    [HttpGet]
    public async Task<IActionResult> Download(int id, int spotId)
    {
        var fileMeta = await _fileService.GetByIdAsync(id);
        if (fileMeta == null) return NotFound();

        if (!fileMeta.IsEncrypted)
        {
            // direct serve public file from wwwroot
            var fileUrl = Url.Content($"~/files/spots/{Uri.EscapeDataString(fileMeta.Name ?? "")}");
            return Redirect(fileUrl);
        }

        // encrypted => show password entry form
        var vm = new DownloadPasswordViewModel { FileId = id, DisplayName = fileMeta.DisplayName, SpotId = spotId };
        return View("EnterPassword", vm);
    }

    // POST: Download with password
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DownloadConfirmed(int id, string password)
    {
        try
        {
            var stream = await ((FileService)_fileService).DecryptFileToStreamAsync(id, password, _env.WebRootPath);
            var fileMeta = await _fileService.GetByIdAsync(id);
            var display = fileMeta?.DisplayName ?? "download.pdf";

            stream.Position = 0;
            // set inline so browser attempts to open it
            Response.Headers.Add("Content-Disposition", $"inline; filename=\"{Uri.EscapeDataString(display)}\"");
            return File(stream, "application/pdf");
        }
        catch (UnauthorizedAccessException)
        {
            TempData["ErrorMessage"] = "Invalid password or corrupted file.";
            return RedirectToAction("Download", new { id, spotId = (await _fileService.GetByIdAsync(id))?.SpotId });
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Unable to download file.";
            return RedirectToAction("Details", "Spots", new { id = (await _fileService.GetByIdAsync(id))?.SpotId });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int spotId)
    {
        await _fileService.DeleteFileAsync(id, _env.WebRootPath);
        TempData["SuccessMessage"] = "File deleted";
        return RedirectToAction("Details", "Spots", new { id = spotId });
    }
}
