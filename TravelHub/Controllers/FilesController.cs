using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Services;

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

        var file = new Domain.Entities.File { Name = fileFile.FileName, SpotId = spotId };
        try
        {
            using (var stream = fileFile.OpenReadStream())
            {
                await _fileService.AddFileAsync(file, stream, fileFile.FileName, _env.WebRootPath);
            }
            TempData["SuccessMessage"] = "File added";
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int spotId)
    {
        await _fileService.DeleteFileAsync(id, _env.WebRootPath);
        TempData["SuccessMessage"] = "File deleted";
        return RedirectToAction("Details", "Spots", new { id = spotId });
    }
}
