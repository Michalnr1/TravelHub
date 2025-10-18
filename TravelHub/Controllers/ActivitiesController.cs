using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Web.ViewModels.Activities;

namespace TravelHub.Web.Controllers;

[Authorize]
public class ActivitiesController : Controller
{
    private readonly IActivityService _activityService;
    private readonly IGenericService<Category> _categoryService;
    private readonly ITripService _tripService;
    private readonly IGenericService<Day> _dayService;

    public ActivitiesController(
        IActivityService activityService,
        IGenericService<Category> categoryService,
        ITripService tripService,
        IGenericService<Day> dayService)
    {
        _activityService = activityService;
        _categoryService = categoryService;
        _tripService = tripService;
        _dayService = dayService;
    }

    // GET: Activities
    public async Task<IActionResult> Index()
    {
        var activities = await _activityService.GetAllAsync();
        var viewModel = activities.Select(a => new ActivityViewModel
        {
            Id = a.Id,
            Name = a.Name,
            Description = a.Description,
            Duration = a.Duration,
            Order = a.Order,
            CategoryName = a.Category?.Name,
            TripName = a.Trip?.Name,
            DayName = a.Day?.Name
        }).ToList();

        return View(viewModel);
    }

    // GET: Activities/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var activity = await _activityService.GetByIdAsync(id.Value);
        if (activity == null)
        {
            return NotFound();
        }

        var viewModel = new ActivityDetailsViewModel
        {
            Id = activity.Id,
            Name = activity.Name,
            Description = activity.Description,
            Duration = activity.Duration,
            Order = activity.Order,
            CategoryName = activity.Category?.Name,
            TripName = activity.Trip?.Name,
            DayName = activity.Day?.Name,
            Type = activity is Spot ? "Spot" : "Activity"
        };

        return View(viewModel);
    }

    // GET: Activities/Create
    public async Task<IActionResult> Create()
    {
        var viewModel = await CreateActivityCreateEditViewModel();
        return View(viewModel);
    }

    // POST: Activities/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ActivityCreateEditViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            var activity = new Activity
            {
                Name = viewModel.Name,
                Description = viewModel.Description,
                Duration = viewModel.Duration,
                Order = viewModel.Order,
                CategoryId = viewModel.CategoryId,
                TripId = viewModel.TripId,
                DayId = viewModel.DayId
            };

            await _activityService.AddAsync(activity);
            return RedirectToAction(nameof(Index));
        }

        await PopulateSelectLists(viewModel);
        return View(viewModel);
    }

    // GET: Activities/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var activity = await _activityService.GetByIdAsync(id.Value);
        if (activity == null)
        {
            return NotFound();
        }

        var viewModel = await CreateActivityCreateEditViewModel(activity);
        return View(viewModel);
    }

    // POST: Activities/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ActivityCreateEditViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var existingActivity = await _activityService.GetByIdAsync(id);
                if (existingActivity == null)
                {
                    return NotFound();
                }

                existingActivity.Name = viewModel.Name;
                existingActivity.Description = viewModel.Description;
                existingActivity.Duration = viewModel.Duration;
                existingActivity.Order = viewModel.Order;
                existingActivity.CategoryId = viewModel.CategoryId;
                existingActivity.TripId = viewModel.TripId;
                existingActivity.DayId = viewModel.DayId;

                await _activityService.UpdateAsync(existingActivity);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ActivityExists(viewModel.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }

        await PopulateSelectLists(viewModel);
        return View(viewModel);
    }

    // GET: Activities/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var activity = await _activityService.GetByIdAsync(id.Value);
        if (activity == null)
        {
            return NotFound();
        }

        var viewModel = new ActivityDetailsViewModel
        {
            Id = activity.Id,
            Name = activity.Name,
            Description = activity.Description,
            Duration = activity.Duration,
            Order = activity.Order,
            CategoryName = activity.Category?.Name,
            TripName = activity.Trip?.Name,
            DayName = activity.Day?.Name,
            Type = activity is Spot ? "Spot" : "Activity"
        };

        return View(viewModel);
    }

    // POST: Activities/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _activityService.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> ActivityExists(int id)
    {
        var activity = await _activityService.GetByIdAsync(id);
        return activity != null;
    }

    private async Task<ActivityCreateEditViewModel> CreateActivityCreateEditViewModel(Activity activity = null)
    {
        var viewModel = new ActivityCreateEditViewModel();

        if (activity != null)
        {
            viewModel.Id = activity.Id;
            viewModel.Name = activity.Name;
            viewModel.Description = activity.Description;
            viewModel.Duration = activity.Duration;
            viewModel.Order = activity.Order;
            viewModel.CategoryId = activity.CategoryId;
            viewModel.TripId = activity.TripId;
            viewModel.DayId = activity.DayId;
        }

        await PopulateSelectLists(viewModel);
        return viewModel;
    }

    private async Task PopulateSelectLists(ActivityCreateEditViewModel viewModel)
    {
        // Categories
        var categories = await _categoryService.GetAllAsync();
        viewModel.Categories = categories.Select(c => new CategorySelectItem
        {
            Id = c.Id,
            Name = c.Name
        }).ToList();

        // Trips
        var trips = await _tripService.GetAllAsync();
        viewModel.Trips = trips.Select(t => new TripSelectItem
        {
            Id = t.Id,
            Name = t.Name
        }).ToList();

        // Days - filter by selected trip if available
        var days = await _dayService.GetAllAsync();
        if (viewModel.TripId > 0)
        {
            days = days.Where(d => d.TripId == viewModel.TripId).ToList();
        }
        viewModel.Days = days.Select(d => new DaySelectItem
        {
            Id = d.Id,
            Number = d.Number,
            Name = d.Name,
            TripId = d.TripId
        }).ToList();
    }
}