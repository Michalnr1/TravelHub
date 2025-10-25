using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Infrastructure.Services;
using TravelHub.Web.ViewModels.Accommodations;
using TravelHub.Web.ViewModels.Activities;
using TravelHub.Web.ViewModels.Expenses;
using TravelHub.Web.ViewModels.Transports;
using TravelHub.Web.ViewModels.Trips;

namespace TravelHub.Web.Controllers;

[Authorize]
public class TripsController : Controller
{
    private readonly ITripService _tripService;
    private readonly ITransportService _transportService;
    private readonly ISpotService _spotService;
    private readonly IActivityService _activityService;
    private readonly ICategoryService _categoryService;
    private readonly IAccommodationService _accommodationService;
    private readonly IExpenseService _expenseService;
    private readonly ILogger<TripsController> _logger;
    private readonly UserManager<Person> _userManager;
    private readonly IConfiguration _configuration;

    public TripsController(ITripService tripService,
        ITransportService transportService,
        ISpotService spotService,
        IActivityService activityService,
        ICategoryService categoryService,
        IAccommodationService accommodationService,
        IExpenseService expenseService,
        ILogger<TripsController> logger,
        UserManager<Person> userManager,
        IConfiguration configuration)
    {
        _tripService = tripService;
        _transportService = transportService;
        _spotService = spotService;
        _activityService = activityService;
        _categoryService = categoryService;
        _accommodationService = accommodationService;
        _expenseService = expenseService;
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
            Person = t.Person!,
            IsPrivate = t.IsPrivate
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

        if (!UserOwnsTrip(trip))
        {
            return Forbid();
        }

        var activities = await _activityService.GetTripActivitiesWithDetailsAsync(id);
        var spots = await _spotService.GetTripSpotsWithDetailsAsync(id);
        var transports = await _transportService.GetTripTransportsWithDetailsAsync(id);
        var accommodations = await _accommodationService.GetAccommodationByTripAsync(id);
        var expenses = await _expenseService.GetByTripIdWithParticipantsAsync(id);

        var viewModel = new TripDetailViewModel
        {
            Id = trip.Id,
            Name = trip.Name,
            Status = trip.Status,
            StartDate = trip.StartDate,
            EndDate = trip.EndDate,
            IsPrivate = trip.IsPrivate,
            Days = trip.Days?.Select(d => new DayViewModel
            {
                Id = d.Id,
                Number = d.Number,
                Name = d.Name,
                Date = d.Date,
                ActivitiesCount = d.Activities?.Count ?? 0
            }).ToList() ?? new List<DayViewModel>(),
            Activities = activities.Select(a => new ActivityViewModel
            {
                Id = a.Id,
                Name = a.Name,
                Description = a.Description ?? string.Empty,
                Duration = a.Duration,
                DurationString = ConvertDecimalToTimeString(a.Duration),
                Order = a.Order,
                CategoryName = a.Category?.Name,
                TripName = a.Trip?.Name ?? string.Empty,
                DayName = a.Day?.Name
            }).ToList(),
            Spots = spots.Select(s => new SpotDetailsViewModel
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description ?? string.Empty,
                Duration = s.Duration,
                DurationString = ConvertDecimalToTimeString(s.Duration),
                Order = s.Order,
                CategoryName = s.Category?.Name,
                TripName = s.Trip?.Name ?? string.Empty,
                DayName = s.Day?.Name,
                Longitude = s.Longitude,
                Latitude = s.Latitude,
                Cost = s.Cost,
                PhotoCount = s.Photos?.Count ?? 0,
                TransportsFromCount = s.TransportsFrom?.Count ?? 0,
                TransportsToCount = s.TransportsTo?.Count ?? 0
            }).ToList(),
            Transports = transports.Select(t => new TransportViewModel
            {
                Id = t.Id,
                Name = t.Name,
                Type = t.Type,
                Duration = t.Duration,
                Cost = t.Cost,
                TripName = t.Trip?.Name ?? string.Empty,
                FromSpotName = t.FromSpot?.Name ?? string.Empty,
                ToSpotName = t.ToSpot?.Name ?? string.Empty
            }).ToList(),
            Accommodations = accommodations.Select(a => new AccommodationViewModel
            {
                Id = a.Id,
                Name = a.Name,
                Description = a.Description ?? string.Empty,
                Cost = a.Cost,
                CategoryName = a.Category?.Name,
                DayName = a.Day?.Name,
                CheckIn = a.CheckIn,
                CheckOut = a.CheckOut,
                Latitude = a.Latitude,
                Longitude = a.Longitude
            }).ToList(),
            Expenses = expenses.Select(e => new ExpenseViewModel
            {
                Id = e.Id,
                Name = e.Name,
                Value = e.Value,
                PaidByName = e.PaidBy != null ? $"{e.PaidBy.FirstName} {e.PaidBy.LastName}" : "Unknown",
                CategoryName = e.Category?.Name,
                CurrencyName = e.Currency?.Name ?? "Unknown"
            }).ToList()
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
                    PersonId = GetCurrentUserId(),
                    IsPrivate = viewModel.IsPrivate
                };

                await _tripService.AddAsync(trip);

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

        if (!UserOwnsTrip(trip))
        {
            return Forbid();
        }

        var viewModel = new EditTripViewModel
        {
            Id = trip.Id,
            Name = trip.Name,
            StartDate = trip.StartDate,
            EndDate = trip.EndDate,
            Status = trip.Status,
            IsPrivate = trip.IsPrivate
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


                if (!UserOwnsTrip(trip))
                {
                    return Forbid();
                }

                trip.Name = viewModel.Name;
                trip.StartDate = viewModel.StartDate;
                trip.EndDate = viewModel.EndDate;
                trip.Status = viewModel.Status;
                trip.IsPrivate = viewModel.IsPrivate;

                await _tripService.UpdateAsync(trip);

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

        if (!UserOwnsTrip(trip))
        {
            return Forbid();
        }

        var viewModel = new TripViewModel
        {
            Id = trip.Id,
            Name = trip.Name,
            Status = trip.Status,
            StartDate = trip.StartDate,
            EndDate = trip.EndDate,
            IsPrivate = trip.IsPrivate
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

        if (!UserOwnsTrip(trip))
        {
            return Forbid();
        }

        await _tripService.DeleteAsync(trip.Id);

        TempData["SuccessMessage"] = "Trip deleted successfully!";
        return RedirectToAction(nameof(MyTrips));
    }

    //// GET: Trips/AddDay/5
    //public async Task<IActionResult> AddDay(int id)
    //{
    //    var trip = await _tripService.GetByIdAsync(id);
    //    if (trip == null)
    //    {
    //        return NotFound();
    //    }

    //    if (!UserOwnsTrip(trip))
    //    {
    //        return Forbid();
    //    }

    //    var viewModel = new AddDayViewModel
    //    {
    //        TripId = id,
    //        TripName = trip.Name,
    //        MinDate = trip.StartDate,
    //        MaxDate = trip.EndDate,
    //        Date = trip.StartDate,
    //        Number = trip.Days.Where(d => d.Number.HasValue).Count() + 1, // Liczymy tylko dni z numerem
    //        IsGroup = false // Domyślnie na 'false' dla dodawania Dnia
    //    };

    //    ViewData["FormTitle"] = "Add New Day";
    //    return View("AddGroup", viewModel);
    //}

    //// POST: Trips/AddDay/5
    //[HttpPost]
    //[ValidateAntiForgeryToken]
    //public async Task<IActionResult> AddDay(int id, AddDayViewModel viewModel)
    //{
    //    // Ustaw IsGroup na false na wypadek, gdyby formularz nie przesłał tej wartości
    //    viewModel.IsGroup = false;

    //    if (id != viewModel.TripId)
    //    {
    //        return NotFound();
    //    }

    //    if (!await _tripService.UserOwnsTripAsync(id, GetCurrentUserId()))
    //    {
    //        return Forbid();
    //    }

    //    // Walidacja: Numer jest wymagany dla Dnia
    //    if (!viewModel.Number.HasValue)
    //    {
    //        ModelState.AddModelError(nameof(viewModel.Number), "Day number is required.");
    //    }

    //    var trip = await _tripService.GetByIdAsync(id);
    //    if (trip == null)
    //    {
    //        return NotFound();
    //    }

    //    if (ModelState.IsValid)
    //    {
    //        try
    //        {
    //            var day = new Day
    //            {
    //                Number = viewModel.Number,
    //                Name = $"Day {viewModel.Number}",
    //                Date = viewModel.Date,
    //                TripId = id
    //            };

    //            await _tripService.AddDayToTripAsync(id, day);

    //            TempData["SuccessMessage"] = "Day added successfully!";
    //            return RedirectToAction(nameof(Details), new { id });
    //        }
    //        catch (ArgumentException ex)
    //        {
    //            ModelState.AddModelError("", ex.Message);
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error adding day to trip");
    //            ModelState.AddModelError("", "An error occurred while adding the day.");
    //        }
    //    }

    //    // Ponownie ustaw właściwości potrzebne dla widoku
    //    viewModel.TripName = trip.Name;
    //    viewModel.MinDate = trip.StartDate;
    //    viewModel.MaxDate = trip.EndDate;

    //    ViewData["FormTitle"] = "Add New Day";
    //    return View("AddGroup", viewModel);
    //}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddDay(int id)
    {
        if (!await _tripService.UserOwnsTripAsync(id, GetCurrentUserId()))
        {
            return Forbid();
        }

        try
        {
            await _tripService.CreateNextDayAsync(id);

            TempData["SuccessMessage"] = "Day added successfully!";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (ArgumentException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            _logger.LogError(ex, "Error creating next day for trip {TripId}", id);
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "An error occurred while automatically adding the day.";
            _logger.LogError(ex, "Generic error creating next day for trip {TripId}", id);
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    // GET: Trips/AddGroup/5
    public async Task<IActionResult> AddGroup(int id)
    {
        var trip = await _tripService.GetByIdAsync(id);
        // ... walidacja i błędy (NotFound, Forbid)

        var viewModel = new AddDayViewModel
        {
            TripId = id,
            TripName = trip.Name,
            MinDate = trip.StartDate,
            MaxDate = trip.EndDate,
            Date = trip.StartDate,
            Number = null,
            IsGroup = true // Domyślnie na 'true' dla dodawania Grupy
        };

        ViewData["FormTitle"] = "Add New Group";
        return View("AddGroup", viewModel); // Używamy wspólnego widoku
    }

    // POST: Trips/AddGroup/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddGroup(int id, AddDayViewModel viewModel)
    {
        // Ustaw IsGroup na true 
        viewModel.IsGroup = true;
        // Walidacja: Numer musi być null
        viewModel.Number = null;

        if (id != viewModel.TripId)
        {
            return NotFound();
        }

        if (!await _tripService.UserOwnsTripAsync(id, GetCurrentUserId()))
        {
            return Forbid();
        }

        var trip = await _tripService.GetByIdAsync(id);
        if (trip == null)
        {
            return NotFound();
        }

        // Walidacja: Nazwa jest wymagana dla Grupy
        if (string.IsNullOrWhiteSpace(viewModel.Name))
        {
            ModelState.AddModelError(nameof(viewModel.Name), "Group name is required.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                var day = new Day
                {
                    Number = null,
                    Name = viewModel.Name,
                    Date = viewModel.Date,
                    TripId = id
                };

                await _tripService.AddDayToTripAsync(id, day);

                TempData["SuccessMessage"] = "Group added successfully!";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding group to trip");
                ModelState.AddModelError("", "An error occurred while adding the group.");
            }
        }

        // Ponownie ustaw właściwości potrzebne dla widoku
        viewModel.TripName = trip.Name;
        viewModel.MinDate = trip.StartDate;
        viewModel.MaxDate = trip.EndDate;

        ViewData["FormTitle"] = "Add New Group";
        return View("AddGroup", viewModel);
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
            IsPrivate = t.IsPrivate,
            DaysCount = (t.Days ?? Enumerable.Empty<Day>()).Where(d => d.Number.HasValue).Count(),
            GroupsCount = (t.Days ?? Enumerable.Empty<Day>()).Where(d => !d.Number.HasValue).Count()
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

    private string ConvertDecimalToTimeString(decimal duration)
    {
        int hours = (int)duration;
        int minutes = (int)((duration - hours) * 60);
        return $"{hours:D2}:{minutes:D2}";
    }

    private bool UserOwnsTrip(Trip trip)
    {
        return trip.PersonId == GetCurrentUserId();
    }
}