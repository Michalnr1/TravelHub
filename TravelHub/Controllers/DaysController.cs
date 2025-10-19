using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Web.ViewModels.Trips;

namespace TravelHub.Web.Controllers;

[Authorize]
public class DaysController : Controller
{
    private readonly IGenericService<Day> _dayService;
    private readonly ITripService _tripService;
    private readonly ILogger<DaysController> _logger;

    public DaysController(IGenericService<Day> dayService, ITripService tripService, ILogger<DaysController> logger)
    {
        _dayService = dayService;
        _tripService = tripService;
        _logger = logger;
    }

    // GET: Days/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var day = await _dayService.GetByIdAsync(id);
        if (day == null)
        {
            return NotFound();
        }

        return View(day);
    }

    // GET: Days/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var day = await _dayService.GetByIdAsync(id);
        if (day == null)
        {
            return NotFound();
        }

        var viewModel = new DayViewModel
        {
            Id = day.Id,
            Number = day.Number,
            Name = day.Name,
            Date = day.Date
        };

        return View(viewModel);
    }

    // POST: Days/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DayViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        try
        {
            var day = await _dayService.GetByIdAsync(id);
            if (day == null)
            {
                return NotFound();
            }

            day.Number = viewModel.Number;
            day.Name = viewModel.Name;
            day.Date = viewModel.Date;

            await _dayService.UpdateAsync(day);

            TempData["SuccessMessage"] = "Day updated successfully!";
            return RedirectToAction(nameof(Details), new { id = day.Id });
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogError("Concurrency error while updating day {Id}", id);
            ModelState.AddModelError("", "Error updating the day, please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating day {Id}", id);
            ModelState.AddModelError("", "An unexpected error occurred.");
        }

        return View(viewModel);
    }

    // GET: Days/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var day = await _dayService.GetByIdAsync(id);
        if (day == null)
        {
            return NotFound();
        }

        return View(day);
    }

    // POST: Days/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            await _dayService.DeleteAsync(id);
            TempData["SuccessMessage"] = "Day deleted successfully!";
            return RedirectToAction("Index", "Trips"); // or redirect back to trip details if preferred
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting day {Id}", id);
            ModelState.AddModelError("", "Error deleting the day.");
            return RedirectToAction(nameof(Delete), new { id });
        }
    }
}
