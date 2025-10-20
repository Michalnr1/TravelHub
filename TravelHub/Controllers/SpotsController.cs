using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Web.ViewModels.Activities;

namespace TravelHub.Web.Controllers;

[Authorize]
public class SpotsController : Controller
{
    private readonly ISpotService _spotService;
    private readonly IGenericService<Category> _categoryService;
    private readonly ITripService _tripService;
    private readonly IGenericService<Day> _dayService;
    private readonly IPhotoService _photoService;

    public SpotsController(
        ISpotService spotService,
        IGenericService<Category> categoryService,
        ITripService tripService,
        IGenericService<Day> dayService,
        IPhotoService photoService)
    {
        _spotService = spotService;
        _categoryService = categoryService;
        _tripService = tripService;
        _dayService = dayService;
        _photoService = photoService;
    }

    // GET: Spots
    public async Task<IActionResult> Index()
    {
        var spots = await _spotService.GetAllAsync();
        var viewModel = spots.Select(s => new SpotDetailsViewModel
        {
            Id = s.Id,
            Name = s.Name,
            Description = s.Description,
            Duration = s.Duration,
            Order = s.Order,
            CategoryName = s.Category?.Name,
            TripName = s.Trip?.Name!,
            DayName = s.Day?.Name,
            Longitude = s.Longitude,
            Latitude = s.Latitude,
            Cost = s.Cost,
            PhotoCount = s.Photos?.Count ?? 0,
            TransportsFromCount = s.TransportsFrom?.Count ?? 0,
            TransportsToCount = s.TransportsTo?.Count ?? 0
        }).ToList();

        return View(viewModel);
    }

    // GET: Spots/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var spot = await _spotService.GetSpotDetailsAsync(id.Value);
        if (spot == null)
        {
            return NotFound();
        }

        var viewModel = new SpotDetailsViewModel
        {
            Id = spot.Id,
            Name = spot.Name,
            Description = spot.Description,
            Duration = spot.Duration,
            Order = spot.Order,
            CategoryName = spot.Category?.Name,
            TripName = spot.Trip?.Name!,
            DayName = spot.Day?.Name,
            Longitude = spot.Longitude,
            Latitude = spot.Latitude,
            Cost = spot.Cost,
            PhotoCount = spot.Photos?.Count ?? 0,
            TransportsFromCount = spot.TransportsFrom?.Count ?? 0,
            TransportsToCount = spot.TransportsTo?.Count ?? 0
        };

        // pobierz zdjęcia i zamapuj na PhotoViewModel
        var photos = await _photoService.GetBySpotIdAsync(spot.Id);
        viewModel.Photos = photos.Select(p => new PhotoViewModel
        {
            Id = p.Id,
            Name = p.Name,
            Alt = p.Alt
        }).ToList();

        return View(viewModel);
    }

    // GET: Spots/Create
    public async Task<IActionResult> Create()
    {
        var viewModel = await CreateSpotCreateEditViewModel();
        return View(viewModel);
    }

    // POST: Spots/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SpotCreateEditViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            var spot = new Spot
            {
                Name = viewModel.Name,
                Description = viewModel.Description!,
                Duration = viewModel.Duration,
                Order = viewModel.Order,
                CategoryId = viewModel.CategoryId,
                TripId = viewModel.TripId,
                DayId = viewModel.DayId,
                Longitude = viewModel.Longitude,
                Latitude = viewModel.Latitude,
                Cost = viewModel.Cost
            };

            await _spotService.AddAsync(spot);
            return RedirectToAction(nameof(Index));
        }

        await PopulateSelectLists(viewModel);
        return View(viewModel);
    }

    // GET: Spots/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var spot = await _spotService.GetByIdAsync(id.Value);
        if (spot == null)
        {
            return NotFound();
        }

        var viewModel = await CreateSpotCreateEditViewModel(spot);
        return View(viewModel);
    }

    // POST: Spots/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SpotCreateEditViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var existingSpot = await _spotService.GetByIdAsync(id);
                if (existingSpot == null)
                {
                    return NotFound();
                }

                existingSpot.Name = viewModel.Name;
                existingSpot.Description = viewModel.Description!;
                existingSpot.Duration = viewModel.Duration;
                existingSpot.Order = viewModel.Order;
                existingSpot.CategoryId = viewModel.CategoryId;
                existingSpot.TripId = viewModel.TripId;
                existingSpot.DayId = viewModel.DayId;
                existingSpot.Longitude = viewModel.Longitude;
                existingSpot.Latitude = viewModel.Latitude;
                existingSpot.Cost = viewModel.Cost;

                await _spotService.UpdateAsync(existingSpot);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await SpotExists(viewModel.Id))
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

    // GET: Spots/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var spot = await _spotService.GetByIdAsync(id.Value);
        if (spot == null)
        {
            return NotFound();
        }

        var viewModel = new SpotDetailsViewModel
        {
            Id = spot.Id,
            Name = spot.Name,
            Description = spot.Description,
            Duration = spot.Duration,
            Order = spot.Order,
            CategoryName = spot.Category?.Name,
            TripName = spot.Trip?.Name!,
            DayName = spot.Day?.Name,
            Longitude = spot.Longitude,
            Latitude = spot.Latitude,
            Cost = spot.Cost
        };

        return View(viewModel);
    }

    // POST: Spots/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _spotService.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> SpotExists(int id)
    {
        var spot = await _spotService.GetByIdAsync(id);
        return spot != null;
    }

    private async Task<SpotCreateEditViewModel> CreateSpotCreateEditViewModel(Spot? spot = null)
    {
        var viewModel = new SpotCreateEditViewModel();

        if (spot != null)
        {
            viewModel.Id = spot.Id;
            viewModel.Name = spot.Name;
            viewModel.Description = spot.Description;
            viewModel.Duration = spot.Duration;
            viewModel.Order = spot.Order;
            viewModel.CategoryId = spot.CategoryId;
            viewModel.TripId = spot.TripId;
            viewModel.DayId = spot.DayId;
            viewModel.Longitude = spot.Longitude;
            viewModel.Latitude = spot.Latitude;
            viewModel.Cost = spot.Cost;
        }

        await PopulateSelectLists(viewModel);
        return viewModel;
    }

    private async Task PopulateSelectLists(SpotCreateEditViewModel viewModel)
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