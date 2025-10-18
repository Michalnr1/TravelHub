// TravelHub.Web/Controllers/TripsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Web.ViewModels.Trips;

namespace TravelHub.Web.Controllers;

[Authorize]
public class TripsController : Controller
{
    private readonly ITripService _tripService;
    private readonly ILogger<TripsController> _logger;

    public TripsController(ITripService tripService, ILogger<TripsController> logger)
    {
        _tripService = tripService;
        _logger = logger;
    }

    // GET: Trips
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        var trips = await _tripService.GetUserTripsAsync(userId);
        var viewModel = trips.Select(t => new TripViewModel
        {
            Id = t.Id,
            Name = t.Name,
            Status = t.Status,
            StartDate = t.StartDate,
            EndDate = t.EndDate,
            DaysCount = t.Days?.Count ?? 0
        });

        return View(viewModel);
    }

    // GET: Trips/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var trip = await _tripService.GetTripWithDetailsAsync(id);
        if (trip == null)
        {
            return NotFound();
        }

        if (!await _tripService.UserOwnsTripAsync(id, GetCurrentUserId()))
        {
            return Forbid();
        }

        var viewModel = new TripDetailViewModel
        {
            Id = trip.Id,
            Name = trip.Name,
            Status = trip.Status,
            StartDate = trip.StartDate,
            EndDate = trip.EndDate,
            Days = trip.Days?.Select(d => new DayViewModel
            {
                Id = d.Id,
                Number = d.Number,
                Date = d.Date,
                ActivitiesCount = d.Activities?.Count ?? 0
            }).ToList() ?? new List<DayViewModel>(),
            TransportsCount = trip.Transports?.Count ?? 0
        };

        return View(viewModel);
    }

    // GET: Trips/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Trips/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTripViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var trip = new Trip
                {
                    Name = viewModel.Name,
                    StartDate = viewModel.StartDate,
                    EndDate = viewModel.EndDate,
                    Status = Status.Planning,
                    PersonId = GetCurrentUserId()
                };

                await _tripService.CreateTripAsync(trip);
                // W rzeczywistej aplikacji tutaj byłoby zapisanie zmian w bazie
                // await _unitOfWork.SaveChangesAsync();

                TempData["SuccessMessage"] = "Trip created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating trip");
                ModelState.AddModelError("", "An error occurred while creating the trip.");
            }
        }

        return View(viewModel);
    }

    // GET: Trips/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var trip = await _tripService.GetTripByIdAsync(id);
        if (trip == null)
        {
            return NotFound();
        }

        if (!await _tripService.UserOwnsTripAsync(id, GetCurrentUserId()))
        {
            return Forbid();
        }

        var viewModel = new EditTripViewModel
        {
            Id = trip.Id,
            Name = trip.Name,
            StartDate = trip.StartDate,
            EndDate = trip.EndDate,
            Status = trip.Status
        };

        return View(viewModel);
    }

    // POST: Trips/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditTripViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var trip = await _tripService.GetTripByIdAsync(id);
                if (trip == null)
                {
                    return NotFound();
                }

                if (!await _tripService.UserOwnsTripAsync(id, GetCurrentUserId()))
                {
                    return Forbid();
                }

                trip.Name = viewModel.Name;
                trip.StartDate = viewModel.StartDate;
                trip.EndDate = viewModel.EndDate;
                trip.Status = viewModel.Status;

                await _tripService.UpdateTripAsync(trip);
                // W rzeczywistej aplikacji tutaj byłoby zapisanie zmian w bazie
                // await _unitOfWork.SaveChangesAsync();

                TempData["SuccessMessage"] = "Trip updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await TripExists(id))
                {
                    return NotFound();
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating trip");
                ModelState.AddModelError("", "An error occurred while updating the trip.");
            }
        }

        return View(viewModel);
    }

    // GET: Trips/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var trip = await _tripService.GetTripByIdAsync(id);
        if (trip == null)
        {
            return NotFound();
        }

        if (!await _tripService.UserOwnsTripAsync(id, GetCurrentUserId()))
        {
            return Forbid();
        }

        var viewModel = new TripViewModel
        {
            Id = trip.Id,
            Name = trip.Name,
            Status = trip.Status,
            StartDate = trip.StartDate,
            EndDate = trip.EndDate
        };

        return View(viewModel);
    }

    // POST: Trips/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var trip = await _tripService.GetTripByIdAsync(id);
        if (trip == null)
        {
            return NotFound();
        }

        if (!await _tripService.UserOwnsTripAsync(id, GetCurrentUserId()))
        {
            return Forbid();
        }

        await _tripService.DeleteTripAsync(trip.Id);
        // W rzeczywistej aplikacji tutaj byłoby zapisanie zmian w bazie
        // await _unitOfWork.SaveChangesAsync();

        TempData["SuccessMessage"] = "Trip deleted successfully!";
        return RedirectToAction(nameof(Index));
    }

    // GET: Trips/AddDay/5
    public async Task<IActionResult> AddDay(int id)
    {
        var trip = await _tripService.GetTripByIdAsync(id);
        if (trip == null)
        {
            return NotFound();
        }

        if (!await _tripService.UserOwnsTripAsync(id, GetCurrentUserId()))
        {
            return Forbid();
        }

        var viewModel = new AddDayViewModel
        {
            TripId = id,
            TripName = trip.Name,
            MinDate = trip.StartDate,
            MaxDate = trip.EndDate
        };

        return View(viewModel);
    }

    // POST: Trips/AddDay/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddDay(int id, AddDayViewModel viewModel)
    {
        if (id != viewModel.TripId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var day = new Day
                {
                    Number = viewModel.Number,
                    Date = viewModel.Date
                };

                await _tripService.AddDayToTripAsync(id, day);
                // W rzeczywistej aplikacji tutaj byłoby zapisanie zmian w bazie
                // await _unitOfWork.SaveChangesAsync();

                TempData["SuccessMessage"] = "Day added successfully!";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding day to trip");
                ModelState.AddModelError("", "An error occurred while adding the day.");
            }
        }

        // Ponownie ustaw właściwości potrzebne dla widoku
        var trip = await _tripService.GetTripByIdAsync(id);
        if (trip != null)
        {
            viewModel.TripName = trip.Name;
            viewModel.MinDate = trip.StartDate;
            viewModel.MaxDate = trip.EndDate;
        }

        return View(viewModel);
    }

    private string GetCurrentUserId()
    {
        // Pobieranie ID zalogowanego użytkownika
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim != null && !string.IsNullOrEmpty(userIdClaim.Value))
        {
            return userIdClaim.Value;
        }

        // Fallback dla różnych typów autentykacji
        userIdClaim = User.FindFirst("sub"); // Dla JWT
        if (userIdClaim != null)
        {
            return userIdClaim.Value;
        }

        // Jeśli używasz Identity, możesz użyć:
        // return _userManager.GetUserId(User);

        throw new UnauthorizedAccessException("User is not authenticated");
    }

    private async Task<bool> TripExists(int id)
    {
        return await _tripService.ExistsAsync(id);
    }
}