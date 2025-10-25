using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Web.ViewModels.Trips;

namespace TravelHub.Web.Controllers;

[Authorize]
public class DaysController : Controller
{
    private readonly IDayService _dayService;
    private readonly ITripService _tripService;
    private readonly UserManager<Person> _userManager;
    private readonly ILogger<DaysController> _logger;

    public DaysController(IDayService dayService,
        ITripService tripService,
        ILogger<DaysController> logger,
        UserManager<Person> userManager)
    {
        _dayService = dayService;
        _tripService = tripService;
        _logger = logger;
        _userManager = userManager;
    }

    // GET: Days/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var day = await _dayService.GetDayWithDetailsAsync(id);
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
    [HttpPost, ActionName("DeleteConfirmed")]
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

    // GET: Day/EditGroup/5
    [HttpGet]
    public async Task<IActionResult> EditGroup(int id)
    {
        var day = await _dayService.GetDayByIdAsync(id);
        if (day == null)
        {
            return NotFound();
        }

        // Sprawdź czy dzień jest grupą
        if (!await _dayService.IsDayAGroupAsync(id))
        {
            return BadRequest("This day is not a group.");
        }

        if (!await _dayService.UserOwnsDayAsync(id, GetCurrentUserId()))
        {
            return Forbid();
        }

        var viewModel = new EditDayViewModel
        {
            Id = day.Id,
            TripId = day.TripId,
            Name = day.Name,
            Date = day.Date,
            IsGroup = true
        };

        var trip = await _tripService.GetByIdAsync(day.TripId);
        if (trip != null)
        {
            viewModel.TripName = trip.Name;
            viewModel.MinDate = trip.StartDate;
            viewModel.MaxDate = trip.EndDate;
        }

        ViewData["FormTitle"] = "Edit Group";
        return View(viewModel);
    }

    // POST: Day/EditGroup/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditGroup(int id, EditDayViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return NotFound();
        }

        // Ustaw IsGroup na true i upewnij się, że Number jest null
        viewModel.IsGroup = true;
        viewModel.Number = null;

        var existingDay = await _dayService.GetDayByIdAsync(id);
        if (existingDay == null)
        {
            return NotFound();
        }

        if (!await _dayService.UserOwnsDayAsync(id, GetCurrentUserId()))
        {
            return Forbid();
        }

        // Sprawdź czy dzień jest grupą
        if (!await _dayService.IsDayAGroupAsync(id))
        {
            ModelState.AddModelError("", "This day is not a group.");
        }

        // Walidacja: Nazwa jest wymagana dla Grupy
        if (string.IsNullOrWhiteSpace(viewModel.Name))
        {
            ModelState.AddModelError(nameof(viewModel.Name), "Group name is required.");
        }

        // Walidacja zakresu daty
        //if (viewModel.Date.HasValue &&
        //    !await _dayService.ValidateDateRangeAsync(existingDay.TripId, viewModel.Date.Value))
        //{
        //    ModelState.AddModelError(nameof(viewModel.Date), "Date must be within the trip date range.");
        //}

        if (ModelState.IsValid)
        {
            try
            {
                existingDay.Name = viewModel.Name;
                existingDay.Date = viewModel.Date;

                await _dayService.UpdateAsync(existingDay);

                TempData["SuccessMessage"] = "Group updated successfully!";
                return RedirectToAction("Details", "Trips", new { id = existingDay.TripId });
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating group");
                ModelState.AddModelError("", "An error occurred while updating the group.");
            }
        }

        // Ponownie ustaw właściwości potrzebne dla widoku
        var trip = await _tripService.GetByIdAsync(existingDay.TripId);
        if (trip != null)
        {
            viewModel.TripName = trip.Name;
            viewModel.MinDate = trip.StartDate;
            viewModel.MaxDate = trip.EndDate;
        }

        ViewData["FormTitle"] = "Edit Group";
        return View(viewModel);
    }

    private string GetCurrentUserId()
    {
        return _userManager.GetUserId(User) ?? throw new UnauthorizedAccessException("User is not authenticated");
    }
}
