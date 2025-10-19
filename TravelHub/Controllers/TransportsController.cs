using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Web.ViewModels.Transports;

namespace TravelHub.Web.Controllers;

[Authorize]
public class TransportsController : Controller
{
    private readonly ITransportService _transportService;
    private readonly ITripService _tripService;
    private readonly ISpotService _spotService;

    public TransportsController(
        ITransportService transportService,
        ITripService tripService,
        ISpotService spotService)
    {
        _transportService = transportService;
        _tripService = tripService;
        _spotService = spotService;
    }

    // GET: Transports
    public async Task<IActionResult> Index()
    {
        var transports = await _transportService.GetAllAsync();
        var viewModel = transports.Select(t => new TransportViewModel
        {
            Id = t.Id,
            Name = t.Name,
            Type = t.Type,
            Duration = t.Duration,
            TripName = t.Trip?.Name!,
            FromSpotName = t.FromSpot?.Name!,
            ToSpotName = t.ToSpot?.Name!
        }).ToList();

        return View(viewModel);
    }

    // GET: Transports/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var transport = await _transportService.GetByIdAsync(id.Value);
        if (transport == null)
        {
            return NotFound();
        }

        var viewModel = new TransportDetailsViewModel
        {
            Id = transport.Id,
            Name = transport.Name,
            Type = transport.Type,
            Duration = transport.Duration,
            TripName = transport.Trip?.Name!,
            FromSpotName = transport.FromSpot?.Name!,
            ToSpotName = transport.ToSpot?.Name!,
            FromSpotCoordinates = transport.FromSpot != null ?
                $"{transport.FromSpot.Latitude:F4}, {transport.FromSpot.Longitude:F4}" : "N/A",
            ToSpotCoordinates = transport.ToSpot != null ?
                $"{transport.ToSpot.Latitude:F4}, {transport.ToSpot.Longitude:F4}" : "N/A"
        };

        return View(viewModel);
    }

    // GET: Transports/Create
    public async Task<IActionResult> Create()
    {
        var viewModel = await CreateTransportCreateEditViewModel();
        return View(viewModel);
    }

    // POST: Transports/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TransportCreateEditViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            // Check if FromSpot and ToSpot are different
            if (viewModel.FromSpotId == viewModel.ToSpotId)
            {
                ModelState.AddModelError("", "From spot and To spot cannot be the same.");
                await PopulateSelectLists(viewModel);
                return View(viewModel);
            }

            var transport = new Transport
            {
                Name = viewModel.Name,
                Type = viewModel.Type,
                Duration = viewModel.Duration,
                TripId = viewModel.TripId,
                FromSpotId = viewModel.FromSpotId,
                ToSpotId = viewModel.ToSpotId
            };

            await _transportService.AddAsync(transport);
            return RedirectToAction(nameof(Index));
        }

        await PopulateSelectLists(viewModel);
        return View(viewModel);
    }

    // GET: Transports/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var transport = await _transportService.GetByIdAsync(id.Value);
        if (transport == null)
        {
            return NotFound();
        }

        var viewModel = await CreateTransportCreateEditViewModel(transport);
        return View(viewModel);
    }

    // POST: Transports/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TransportCreateEditViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            // Check if FromSpot and ToSpot are different
            if (viewModel.FromSpotId == viewModel.ToSpotId)
            {
                ModelState.AddModelError("", "From spot and To spot cannot be the same.");
                await PopulateSelectLists(viewModel);
                return View(viewModel);
            }

            try
            {
                var existingTransport = await _transportService.GetByIdAsync(id);
                if (existingTransport == null)
                {
                    return NotFound();
                }

                existingTransport.Name = viewModel.Name;
                existingTransport.Type = viewModel.Type;
                existingTransport.Duration = viewModel.Duration;
                existingTransport.TripId = viewModel.TripId;
                existingTransport.FromSpotId = viewModel.FromSpotId;
                existingTransport.ToSpotId = viewModel.ToSpotId;

                await _transportService.UpdateAsync(existingTransport);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await TransportExists(viewModel.Id))
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

    // GET: Transports/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var transport = await _transportService.GetByIdAsync(id.Value);
        if (transport == null)
        {
            return NotFound();
        }

        var viewModel = new TransportDetailsViewModel
        {
            Id = transport.Id,
            Name = transport.Name,
            Type = transport.Type,
            Duration = transport.Duration,
            TripName = transport.Trip?.Name!,
            FromSpotName = transport.FromSpot?.Name!,
            ToSpotName = transport.ToSpot?.Name!
        };

        return View(viewModel);
    }

    // POST: Transports/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _transportService.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> TransportExists(int id)
    {
        var transport = await _transportService.GetByIdAsync(id);
        return transport != null;
    }

    private async Task<TransportCreateEditViewModel> CreateTransportCreateEditViewModel(Transport? transport = null)
    {
        var viewModel = new TransportCreateEditViewModel();

        if (transport != null)
        {
            viewModel.Id = transport.Id;
            viewModel.Name = transport.Name;
            viewModel.Type = transport.Type;
            viewModel.Duration = transport.Duration;
            viewModel.TripId = transport.TripId;
            viewModel.FromSpotId = transport.FromSpotId;
            viewModel.ToSpotId = transport.ToSpotId;
        }

        await PopulateSelectLists(viewModel);
        return viewModel;
    }

    private async Task PopulateSelectLists(TransportCreateEditViewModel viewModel)
    {
        // Trips
        var trips = await _tripService.GetAllAsync();
        viewModel.Trips = trips.Select(t => new TripSelectItem
        {
            Id = t.Id,
            Name = t.Name
        }).ToList();

        // Spots - filter by selected trip if available
        var spots = await _spotService.GetAllAsync();
        if (viewModel.TripId > 0)
        {
            spots = spots.Where(s => s.TripId == viewModel.TripId).ToList();
        }
        viewModel.Spots = spots.Select(s => new SpotSelectItem
        {
            Id = s.Id,
            Name = s.Name,
            TripId = s.TripId,
            Coordinates = $"{s.Latitude:F4}, {s.Longitude:F4}"
        }).ToList();

        // Transportation types
        viewModel.TransportationTypes = Enum.GetValues(typeof(TransportationType))
            .Cast<TransportationType>()
            .Select(t => new TransportationTypeSelectItem
            {
                Value = t,
                Name = t.ToString()
            }).ToList();
    }

    // AJAX method to get spots for a trip
    public async Task<JsonResult> GetSpotsByTrip(int tripId)
    {
        var spots = await _spotService.GetSpotsByTripAsync(tripId);
        var spotList = spots.Select(s => new SpotSelectItem
        {
            Id = s.Id,
            Name = s.Name,
            TripId = s.TripId,
            Coordinates = $"{s.Latitude:F4}, {s.Longitude:F4}"
        }).ToList();

        return Json(spotList);
    }
}