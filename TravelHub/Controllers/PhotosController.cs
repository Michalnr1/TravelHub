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

    public PhotosController(IPhotoService photoService, IWebHostEnvironment env)
    {
        _photoService = photoService;
        _env = env;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(int spotId, IFormFile photoFile, string? alt)
    {
        if (photoFile == null || photoFile.Length == 0)
        {
            TempData["ErrorMessage"] = "Brak pliku do przesłania.";
            return RedirectToAction("Details", "Spots", new { id = spotId });
        }

        var photo = new Photo { Name = photoFile.FileName, SpotId = spotId, Alt = alt };
        try
        {
            using (var stream = photoFile.OpenReadStream())
            {
                await _photoService.AddPhotoAsync(photo, stream, photoFile.FileName, _env.WebRootPath);
            }
            TempData["SuccessMessage"] = "Zdjęcie dodane.";
        }
        catch (ArgumentException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Wystąpił błąd podczas dodawania zdjęcia.";
        }

        return RedirectToAction("Details", "Spots", new { id = spotId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int spotId)
    {
        await _photoService.DeletePhotoAsync(id, _env.WebRootPath);
        TempData["SuccessMessage"] = "Zdjęcie usunięte.";
        return RedirectToAction("Details", "Spots", new { id = spotId });
    }
}