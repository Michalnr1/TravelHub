using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Web.ViewModels.Activities;
using TravelHub.Web.ViewModels.Trips;

namespace TravelHub.Web.Controllers;

[Authorize]
public class TripsController : Controller
{
    private readonly ITripService _tripService;
    private readonly ISpotService _spotService;
    private readonly IActivityService _activityService;
    private readonly IGenericService<Category> _categoryService;
    private readonly ILogger<TripsController> _logger;
    private readonly UserManager<Person> _userManager;
    private readonly IConfiguration _configuration;

    public TripsController(ITripService tripService, ISpotService spotService, IActivityService activityService, IGenericService<Category> categoryService, ILogger<TripsController> logger, UserManager<Person> userManager, IConfiguration configuration)
    {
        _tripService = tripService;
        _spotService = spotService;
        _activityService = activityService;
        _categoryService = categoryService;
        _configuration = configuration;
        _logger = logger;
        _userManager = userManager;
    }

    // GET: Trips
    public async Task<IActionResult> Index()
    {
        var trips = await _tripService.GetAllWithUserAsync();
        var viewModel = trips.Select(t => new TripWithUserViewModel
        {
            Id = t.Id,
            Name = t.Name,
            Status = t.Status,
            StartDate = t.StartDate,
            EndDate = t.EndDate,
            DaysCount = t.Days?.Count ?? 0,
            Person = t.Person
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
                Name = d.Name,
                Date = d.Date,
                ActivitiesCount = d.Activities?.Count ?? 0
            }).ToList() ?? new List<DayViewModel>(),
            Activities = trip.Activities?.Select(d => new BasicActivityViewModel
            {
                Name = d.Name,
                Description = d.Description,
                Duration = d.Duration,
                CategoryName = d.Category?.Name
            }).ToList() ?? new List<BasicActivityViewModel>(),
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

                await _tripService.AddAsync(trip);
                // W rzeczywistej aplikacji tutaj byłoby zapisanie zmian w bazie
                // await _unitOfWork.SaveChangesAsync();

                TempData["SuccessMessage"] = "Trip created successfully!";
                return RedirectToAction(nameof(MyTrips));
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
        var trip = await _tripService.GetByIdAsync(id);
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
                var trip = await _tripService.GetByIdAsync(id);
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

                await _tripService.UpdateAsync(trip);
                // W rzeczywistej aplikacji tutaj byłoby zapisanie zmian w bazie
                // await _unitOfWork.SaveChangesAsync();

                TempData["SuccessMessage"] = "Trip updated successfully!";
                return RedirectToAction(nameof(MyTrips));
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
        var trip = await _tripService.GetByIdAsync(id);
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
        var trip = await _tripService.GetByIdAsync(id);
        if (trip == null)
        {
            return NotFound();
        }

        if (!await _tripService.UserOwnsTripAsync(id, GetCurrentUserId()))
        {
            return Forbid();
        }

        await _tripService.DeleteAsync(trip.Id);
        // W rzeczywistej aplikacji tutaj byłoby zapisanie zmian w bazie
        // await _unitOfWork.SaveChangesAsync();

        TempData["SuccessMessage"] = "Trip deleted successfully!";
        return RedirectToAction(nameof(MyTrips));
    }

    // GET: Trips/AddDay/5
    public async Task<IActionResult> AddDay(int id)
    {
        var trip = await _tripService.GetByIdAsync(id);
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
                    Name = viewModel.Name,
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
        var trip = await _tripService.GetByIdAsync(id);
        if (trip != null)
        {
            viewModel.TripName = trip.Name;
            viewModel.MinDate = trip.StartDate;
            viewModel.MaxDate = trip.EndDate;
        }

        return View(viewModel);
    }

    public async Task<IActionResult> AddSpot(int id)
    {
        var trip = await _tripService.GetByIdAsync(id);
        if (trip == null)
        {
            return NotFound();
        }

        if (!await _tripService.UserOwnsTripAsync(id, GetCurrentUserId()))
        {
            return Forbid();
        }

        var viewModel = new SpotCreateEditViewModel
        {
            TripId = id,
            Order = 1
        };

        // Categories
        var categories = await _categoryService.GetAllAsync();
        viewModel.Categories = categories.Select(c => new CategorySelectItem
        {
            Id = c.Id,
            Name = c.Name
        }).ToList();

        ViewData["GoogleApiKey"] = _configuration["ApiKeys:GoogleApiKey"];

        (double lat, double lng) = await _tripService.GetMedianCoords(id);

        ViewData["Latitude"] = lat;
        ViewData["Longitude"] = lng;

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSpot(int id, SpotCreateEditViewModel viewModel)
    {

        if (ModelState.IsValid)
        {
            try
            {
                var spot = new Spot
                {
                    Name = viewModel.Name,
                    Description = viewModel.Description,
                    Duration = viewModel.Duration,
                    CategoryId = viewModel.CategoryId,
                    TripId = id,
                    Longitude = viewModel.Longitude,
                    Latitude = viewModel.Latitude,
                    Cost = viewModel.Cost
                };

                await _spotService.AddAsync(spot);

                TempData["SuccessMessage"] = "Spot added successfully!";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding spot to trip");
                ModelState.AddModelError("", "An error occurred while adding the day.");
            }
        }

        // Ponownie ustaw właściwości potrzebne dla widoku
        var trip = await _tripService.GetByIdAsync(id);
        if (trip != null)
        {
            viewModel.TripId = id;
            viewModel.Order = 1;
        }

        // Categories
        var categories = await _categoryService.GetAllAsync();
        viewModel.Categories = categories.Select(c => new CategorySelectItem
        {
            Id = c.Id,
            Name = c.Name
        }).ToList();

        ViewData["GoogleApiKey"] = _configuration["ApiKeys:GoogleApiKey"];


        return View(viewModel);
    }

    public async Task<IActionResult> AddActivity(int id)
    {
        var trip = await _tripService.GetByIdAsync(id);
        if (trip == null)
        {
            return NotFound();
        }

        if (!await _tripService.UserOwnsTripAsync(id, GetCurrentUserId()))
        {
            return Forbid();
        }

        var viewModel = new ActivityCreateEditViewModel
        {
            TripId = id,
            Order = 1
        };

        // Categories
        var categories = await _categoryService.GetAllAsync();
        viewModel.Categories = categories.Select(c => new CategorySelectItem
        {
            Id = c.Id,
            Name = c.Name
        }).ToList();

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddActivity(int id, ActivityCreateEditViewModel viewModel)
    {

        if (ModelState.IsValid)
        {
            try
            {
                var activity = new Activity
                {
                    Name = viewModel.Name,
                    Description = viewModel.Description,
                    Duration = viewModel.Duration,
                    CategoryId = viewModel.CategoryId,
                    TripId = id,
                };

                await _activityService.AddAsync(activity);

                TempData["SuccessMessage"] = "Activity added successfully!";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding activity to trip");
                ModelState.AddModelError("", "An error occurred while adding the activity.");
            }
        }

        // Ponownie ustaw właściwości potrzebne dla widoku
        var trip = await _tripService.GetByIdAsync(id);
        if (trip != null)
        {
            viewModel.TripId = id;
            viewModel.Order = 1;
        }

        // Categories
        var categories = await _categoryService.GetAllAsync();
        viewModel.Categories = categories.Select(c => new CategorySelectItem
        {
            Id = c.Id,
            Name = c.Name
        }).ToList();

        return View(viewModel);
    }

    // GET: MyTrips
    public async Task<IActionResult> MyTrips()
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

    private string GetCurrentUserId()
    {
        return _userManager.GetUserId(User) ?? throw new UnauthorizedAccessException("User is not authenticated");
    }

    private async Task<bool> TripExists(int id)
    {
        return await _tripService.ExistsAsync(id);
    }
}