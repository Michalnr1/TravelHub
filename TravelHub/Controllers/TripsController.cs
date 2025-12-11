using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using TravelHub.Domain.DTOs;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Web.ViewModels.Accommodations;
using TravelHub.Web.ViewModels.Activities;
using TravelHub.Web.ViewModels.Checklists;
using TravelHub.Web.ViewModels.Expenses;
using TravelHub.Web.ViewModels.Transports;
using TravelHub.Web.ViewModels.Trips;

namespace TravelHub.Web.Controllers;

[Authorize]
public class TripsController : Controller
{
    private readonly ITripService _tripService;
    private readonly ITripParticipantService _tripParticipantService;
    private readonly ITransportService _transportService;
    private readonly ISpotService _spotService;
    private readonly IActivityService _activityService;
    private readonly ICategoryService _categoryService;
    private readonly IAccommodationService _accommodationService;
    private readonly IExpenseService _expenseService;
    private readonly IFlightService _flightService;
    private readonly IFlightInfoService _flightInfoService;
    private readonly IRecommendationService _recommendationService;
    private readonly ILogger<TripsController> _logger;
    private readonly UserManager<Person> _userManager;
    private readonly IConfiguration _configuration;

    public TripsController(ITripService tripService,
        ITripParticipantService tripParticipantService,
        ITransportService transportService,
        ISpotService spotService,
        IActivityService activityService,
        ICategoryService categoryService,
        IAccommodationService accommodationService,
        IExpenseService expenseService,
        IFlightService flightService,
        IFlightInfoService flightInfoService,
        IRecommendationService recommendationService,
        ILogger<TripsController> logger,
        UserManager<Person> userManager,
        IConfiguration configuration)
    {
        _tripService = tripService;
        _tripParticipantService = tripParticipantService;
        _transportService = transportService;
        _spotService = spotService;
        _activityService = activityService;
        _categoryService = categoryService;
        _accommodationService = accommodationService;
        _expenseService = expenseService;
        _flightService = flightService;
        _flightInfoService = flightInfoService;
        _recommendationService = recommendationService;
        _configuration = configuration;
        _logger = logger;
        _userManager = userManager;
    }

    // GET: Trips
    public async Task<IActionResult> Index()
    {
        var trips = await _tripService.GetAllWithUserAsync();
        var viewModel = trips.Select(async t => new TripWithUserViewModel
        {
            Id = t.Id,
            Name = t.Name,
            Status = t.Status,
            StartDate = t.StartDate,
            EndDate = t.EndDate,
            DaysCount = t.Days?.Count ?? 0,
            Person = t.Person!,
            IsPrivate = t.IsPrivate,
            ParticipantsCount = await _tripParticipantService.GetParticipantCountAsync(t.Id)
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

        if (!await _tripParticipantService.UserHasAccessToTripAsync(id, GetCurrentUserId()))
        {
            return Forbid();
        }

        var activities = await _activityService.GetTripActivitiesWithDetailsAsync(id);
        var spots = await _spotService.GetTripSpotsWithDetailsAsync(id);
        var transports = await _transportService.GetTripTransportsWithDetailsAsync(id);
        var accommodations = await _accommodationService.GetAccommodationByTripAsync(id);
        var expenses = await _expenseService.GetByTripIdWithParticipantsAsync(id);

        // Pobierz uczestników i dostępnych znajomych
        var participants = await _tripParticipantService.GetTripParticipantsAsync(id);
        var availableFriends = await _tripParticipantService.GetFriendsAvailableForTripAsync(id, GetCurrentUserId());

        // Oblicz całkowite wydatki w walucie podróży
        var expensesSummary = await _expenseService.CalculateTripExpensesInTripCurrencyAsync(id, trip.CurrencyCode);

        // Mapowanie expenses z obliczonymi wartościami
        var expenseViewModels = expenses.Select(e =>
        {
            var calculation = expensesSummary.ExpenseCalculations
                .FirstOrDefault(calc => calc.ExpenseId == e.Id);

            return new ExpenseViewModel
            {
                Id = e.Id,
                Name = e.Name,
                Value = e.Value,
                EstimatedValue = e.EstimatedValue,
                PaidByName = e.PaidBy != null ? $"{e.PaidBy.FirstName} {e.PaidBy.LastName}" : "Unknown",
                TransferredToName = e.TransferredTo == null ? null : e.TransferredTo.FirstName + " " + e.TransferredTo.LastName,
                CategoryName = e.Category?.Name,
                CurrencyName = e.ExchangeRate?.Name ?? trip.CurrencyCode.GetDisplayName(),
                CurrencyCode = e.ExchangeRate?.CurrencyCodeKey ?? trip.CurrencyCode,
                ExchangeRateValue = e.ExchangeRate?.ExchangeRateValue ?? 1m,
                ConvertedValue = calculation?.ConvertedValue ?? e.Value,
                IsEstimated = e.IsEstimated,
                Multiplier = e.Multiplier,
                SpotId = e.SpotId,
                SpotName = e.Spot?.Name,
                TransportId = e.TransportId,
                TransportName = e.Transport?.Name
            };
        }).ToList();

        var totalActualExpenses = expenseViewModels
        .Where(e => !e.IsEstimated && string.IsNullOrEmpty(e.TransferredToName))
        .Sum(e => e.ConvertedValue);

        var totalEstimatedExpenses = expenseViewModels
            .Where(e => e.IsEstimated)
            .Sum(e => e.ConvertedValue * e.Multiplier);

        var viewModel = new TripDetailViewModel
        {
            Id = trip.Id,
            Name = trip.Name,
            Status = trip.Status,
            StartDate = trip.StartDate,
            EndDate = trip.EndDate,
            IsPrivate = trip.IsPrivate,
            CurrencyCode = trip.CurrencyCode,
            //TotalExpenses = expensesSummary.TotalExpensesInTripCurrency,
            OwnerId = trip.PersonId,
            OwnerName = $"{trip.Person?.FirstName} {trip.Person?.LastName}",
            IsCurrentUserOwner = UserOwnsTrip(trip),
            Participants = participants.Select(p => new TripParticipantViewModel
            {
                Id = p.Id,
                PersonId = p.PersonId,
                FirstName = p.Person.FirstName,
                LastName = p.Person.LastName,
                Email = p.Person.Email!,
                Status = p.Status,
                JoinedAt = p.JoinedAt,
                IsOwner = p.PersonId == trip.PersonId
            }).ToList(),
            AvailableFriends = availableFriends.Select(f => new FriendViewModel
            {
                Id = f.Id,
                FirstName = f.FirstName,
                LastName = f.LastName,
                Email = f.Email!
            }).ToList(),
            Days = trip.Days?.Select(d => new DayViewModel
            {
                Id = d.Id,
                Number = d.Number,
                Name = d.Name,
                Date = d.Date,
                ActivitiesCount = d.Activities?.Count ?? 0
            }).ToList() ?? new List<DayViewModel>(),
            Activities = activities
            .Where(a => a is not Spot)
            .OrderBy(a => a.Day == null)
            .ThenBy(a => a.Day?.Number != null ? a.Day.Number : int.MaxValue)
            .ThenBy(a => a.Day?.Number == null ? a.Day?.Name : null)
            .ThenBy(a => a.Order)
            .Select(a => new ActivityViewModel
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
            Spots = spots
            .Where(s => s is not Accommodation)
            .OrderBy(s => s.Day == null)
            .ThenBy(s => s.Day?.Number != null ? s.Day.Number : int.MaxValue)
            .ThenBy(s => s.Day?.Number == null ? s.Day?.Name : null)
            .ThenBy(s => s.Order)
            .Select(s => new SpotDetailsViewModel
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
                // Cost = s.Cost,
                PhotoCount = s.Photos?.Count ?? 0
            }).ToList(),
            Transports = transports.Select(t => new TransportViewModel
            {
                Id = t.Id,
                Name = t.Name,
                Type = t.Type,
                Duration = t.Duration,
                DurationString = ConvertDecimalToTimeString(t.Duration),
                // Cost = t.Cost,
                TripName = t.Trip?.Name ?? string.Empty,
                FromSpotName = t.FromSpot?.Name ?? string.Empty,
                ToSpotName = t.ToSpot?.Name ?? string.Empty
            }).ToList(),
            Accommodations = accommodations.Select(a => new AccommodationViewModel
            {
                Id = a.Id,
                Name = a.Name,
                Description = a.Description ?? string.Empty,
                // Cost = a.Cost,
                CategoryName = a.Category?.Name,
                DayName = a.Day?.Name,
                Days = a.Days,
                CheckIn = a.CheckIn,
                CheckOut = a.CheckOut,
                Latitude = a.Latitude,
                Longitude = a.Longitude
            }).ToList(),
            Expenses = expenseViewModels
        };

        // Sprawdź czy blog istnieje i przekaż do widoku
        var hasBlog = await _tripService.HasBlogAsync(id);
        ViewData["HasBlog"] = hasBlog;

        // Sprawdź czy użytkownik jest właścicielem
        var currentUserId = _userManager.GetUserId(User);
        var isOwner = trip.PersonId == currentUserId;
        ViewData["IsOwner"] = isOwner;

        return View(viewModel);
    }

    // GET: Trips/Participants/5
    public async Task<IActionResult> Participants(int id)
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

        var participants = await _tripParticipantService.GetTripParticipantsAsync(id);
        var availableFriends = await _tripParticipantService.GetFriendsAvailableForTripAsync(id, GetCurrentUserId());

        var viewModel = new TripDetailViewModel
        {
            Id = trip.Id,
            Name = trip.Name,
            OwnerId = trip.PersonId,
            OwnerName = $"{trip.Person?.FirstName} {trip.Person?.LastName}",
            Participants = participants.Select(p => new TripParticipantViewModel
            {
                Id = p.Id,
                PersonId = p.PersonId,
                FirstName = p.Person.FirstName,
                LastName = p.Person.LastName,
                Email = p.Person.Email!,
                Status = p.Status,
                JoinedAt = p.JoinedAt,
                IsOwner = p.PersonId == trip.PersonId
            }).ToList(),
            AvailableFriends = availableFriends.Select(f => new FriendViewModel
            {
                Id = f.Id,
                FirstName = f.FirstName,
                LastName = f.LastName,
                Email = f.Email!
            }).ToList(),
            IsCurrentUserOwner = GetCurrentUserId() == trip.PersonId
        };

        return View(viewModel);
    }

    // POST: Trips/AddParticipant
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddParticipant(AddParticipantViewModel viewModel)
    {
        var trip = await _tripService.GetByIdAsync(viewModel.TripId);
        if (trip == null)
        {
            return NotFound();
        }

        if (!UserOwnsTrip(trip))
        {
            return Forbid();
        }

        if (ModelState.IsValid)
        {
            try
            {
                // Sprawdź czy użytkownik jest dostępnym znajomym
                var availableFriends = await _tripParticipantService.GetFriendsAvailableForTripAsync(viewModel.TripId, GetCurrentUserId());
                if (!availableFriends.Any(f => f.Id == viewModel.SelectedFriendId))
                {
                    TempData["ErrorMessage"] = "Selected user is not your friend or is already a participant.";
                    return RedirectToAction(nameof(Participants), new { id = viewModel.TripId });
                }

                await _tripParticipantService.AddParticipantAsync(viewModel.TripId, viewModel.SelectedFriendId);

                TempData["SuccessMessage"] = "Participant added successfully! Invitation email sent.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding participant to trip {TripId}", viewModel.TripId);
                TempData["ErrorMessage"] = "An error occurred while adding the participant.";
            }
        }

        return RedirectToAction(nameof(Participants), new { id = viewModel.TripId });
    }

    // POST: Trips/RemoveParticipant
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveParticipant(int tripId, string personId)
    {
        var trip = await _tripService.GetByIdAsync(tripId);
        if (trip == null)
        {
            return NotFound();
        }

        if (!UserOwnsTrip(trip))
        {
            return Forbid();
        }

        var result = await _tripParticipantService.RemoveParticipantAsync(tripId, personId);
        if (result)
        {
            TempData["SuccessMessage"] = "Participant removed successfully!";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to remove participant.";
        }

        return RedirectToAction(nameof(Participants), new { id = tripId });
    }

    // POST: Trips/AcceptInvitation
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptInvitation(int participantId)
    {
        var participant = await _tripParticipantService.GetByIdAsync(participantId);
        if (participant == null)
        {
            return NotFound();
        }

        if (participant.PersonId != GetCurrentUserId())
        {
            return Forbid();
        }

        var result = await _tripParticipantService.UpdateParticipantStatusAsync(participantId, TripParticipantStatus.Accepted);
        if (result)
        {
            TempData["SuccessMessage"] = "Trip invitation accepted!";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to accept invitation.";
        }

        return RedirectToAction(nameof(MyTrips));
    }

    // POST: Trips/DeclineInvitation
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeclineInvitation(int participantId)
    {
        var participant = await _tripParticipantService.GetByIdAsync(participantId);
        if (participant == null)
        {
            return NotFound();
        }

        if (participant.PersonId != GetCurrentUserId())
        {
            return Forbid();
        }

        var result = await _tripParticipantService.UpdateParticipantStatusAsync(participantId, TripParticipantStatus.Declined);
        if (result)
        {
            TempData["SuccessMessage"] = "Trip invitation declined.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to decline invitation.";
        }

        return RedirectToAction(nameof(MyTrips));
    }

    public async Task<IActionResult> MapView(int id, string source = "")
    {
        var trip = await _tripService.GetTripWithDetailsAsync(id);
        if (trip == null)
        {
            return NotFound();
        }

        if (source != "public" && !await _tripParticipantService.UserHasAccessToTripAsync(id, GetCurrentUserId()))
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
            Activities = activities
            .Where(a => a is not Spot)
            .OrderBy(a => a.Day == null)
            .ThenBy(a => a.Day?.Number != null ? a.Day.Number : int.MaxValue)
            .ThenBy(a => a.Day?.Number == null ? a.Day?.Name : null)
            .ThenBy(a => a.Order)
            .Select(a => new ActivityViewModel
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
            Spots = spots
            .Where(s => s is not Accommodation)
            .OrderBy(s => s.Day == null)
            .ThenBy(s => s.Day?.Number != null ? s.Day.Number : int.MaxValue)
            .ThenBy(s => s.Day?.Number == null ? s.Day?.Name : null)
            .ThenBy(s => s.Order)
            .Select(s => new SpotDetailsViewModel
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
                // Cost = s.Cost,
                PhotoCount = s.Photos?.Count ?? 0
            }).ToList(),
            Transports = transports.Select(t => new TransportViewModel
            {
                Id = t.Id,
                Name = t.Name,
                Type = t.Type,
                Duration = t.Duration,
                // Cost = t.Cost,
                TripName = t.Trip?.Name ?? string.Empty,
                FromSpotName = t.FromSpot?.Name ?? string.Empty,
                ToSpotName = t.ToSpot?.Name ?? string.Empty
            }).ToList(),
            Accommodations = accommodations.Select(a => new AccommodationViewModel
            {
                Id = a.Id,
                Name = a.Name,
                Description = a.Description ?? string.Empty,
                // Cost = a.Cost,
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
                CurrencyName = e.ExchangeRate?.Name ?? "Unknown"
            }).ToList()
        };

        ViewData["GoogleApiKey"] = _configuration["ApiKeys:GoogleApiKey"];

        (double lat, double lng) = await _tripService.GetMedianCoordinates(id);

        ViewData["Latitude"] = lat;
        ViewData["Longitude"] = lng;

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
                    IsPrivate = viewModel.IsPrivate,
                    CurrencyCode = viewModel.CurrencyCode
                };

                var newTrip = await _tripService.AddAsync(trip);
                await _tripParticipantService.AddOwnerAsync(newTrip.Id, GetCurrentUserId());

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
            IsPrivate = trip.IsPrivate,
            CurrencyCode = trip.CurrencyCode
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
                trip.CurrencyCode = viewModel.CurrencyCode;

                await _tripService.UpdateAsync(trip);
                // Nie koniecznie potrzebne
                await _tripParticipantService.AddOwnerAsync(trip.Id, trip.PersonId);

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddDay(int id)
    {
        if (!await _tripParticipantService.UserHasAccessToTripAsync(id, GetCurrentUserId()))
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
        if (!await _tripParticipantService.UserHasAccessToTripAsync(id, GetCurrentUserId()))
        {
            return Forbid();
        }

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

    public async Task<IActionResult> FlightSearch(int id)
    {
        Trip trip = await _tripService.GetByIdAsync(id);
        if (trip == null)
        {
            return NotFound();
        }
        (double lat, double lng) = await _tripService.GetMedianCoordinates(id);
        AirportDto? airport = await _flightService.GetAirportByCoords(lat, lng);
        Person? user = await _userManager.GetUserAsync(User);
        string fromAirport;
        string toAirport;
        if (user != null && user.DefaultAirportCode != null)
        {
            fromAirport = user.DefaultAirportCode;
        }
        else
        {
            fromAirport = "";
        }
        if (airport != null && airport.AirportCode != null)
        {
            toAirport = airport.AirportCode;
        }
        else
        {
            toAirport = "";
        }
        var allCurrencyCodes = Enum.GetValues(typeof(CurrencyCode))
            .Cast<CurrencyCode>()
            .Select(currency => new CurrencySelectGroupItem
            {
                Key = currency,
                Name = currency.GetDisplayName()
            })
            .ToList();
        FlightSearchViewModel model = new FlightSearchViewModel
        {
            TripId = id,
            FromAirportCode = fromAirport,
            ToAirportCode = toAirport,
            DefaultCurrencyCode = trip.CurrencyCode,
            Currencies = allCurrencyCodes,
            DefaultDate = trip.StartDate,
            Participants = trip.Participants.Count
        };      
        return View(model);
    }

    public async Task<IActionResult> Flights(string from, string to, string date, int? adults, int? children, int? seatedInfants, int? heldInfants,
                                            string? currency, int? maxPrice, int? maxStops)
    {
        if (from == null || to == null || date == null) { return BadRequest(); }
        if (maxPrice != null && currency == null) { return BadRequest(); }
        if (maxStops > 2 || maxStops < 0) { return BadRequest(); }
        bool dateValid = DateTime.TryParseExact(date, "yyyy-MM-dd", null, 0, out DateTime result);
        if (!dateValid) { return BadRequest(); }
        try
        {
            var flights = await _flightService.GetFlights(from, to, result, adults, children, seatedInfants, heldInfants, currency, 
                                                          maxPrice, maxStops);
            return Ok(flights);
        } catch (HttpRequestException)
        {
            return BadRequest();
        }  
    }

    public async Task<IActionResult> Airports(string query)
    {
        if (query == null) { return BadRequest(); }
        try
        {
            var airports = await _flightService.GetAirportsByName(query);
            return Ok(airports);
        }
        catch (HttpRequestException)
        {
            return BadRequest();
        }
    }

    [HttpGet]
    public async Task<IActionResult> Recommendations(int id)
    {
        try
        {
            (double latitude, double longitude) = await _tripService.GetMedianCoordinates(id);
            var existingSpots = await _spotService.GetSpotsByTripAsync(id);

            ViewData["Latitude"] = latitude;
            ViewData["Longitude"] = longitude;
            ViewData["TripId"] = id;
            ViewData["ExistingSpots"] = existingSpots;
            return View("Recommendations");
        }
        catch (Exception)
        {
            // Log error
            return RedirectToAction("Error", "Home");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetRecommendations(
        [FromQuery] double lat,
        [FromQuery] double lng,
        [FromQuery] int radius = 5000)
    {
        try
        {
            if (Math.Abs(lat) > 90 || Math.Abs(lng) > 180)
            {
                return BadRequest("Invalid coordinates provided.");
            }

            if (radius < 100 || radius > 50000)
            {
                return BadRequest("Radius must be between 100 and 50,000 meters.");
            }

            var recommendations = await _recommendationService.FindRecommendationsByCoordinates(lat, lng, radius);
            return Ok(recommendations);
        }
        catch (HttpRequestException)
        {
            // Log the exception
            return BadRequest("Unable to fetch recommendations. Please try again later.");
        }
        catch (Exception)
        {
            // Log the exception
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetPlacePhoto([FromQuery] string name)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Photo name is required.");
            }

            var photoUrl = await _recommendationService.GetPhotoUrl(name);

            if (string.IsNullOrEmpty(photoUrl))
            {
                return NotFound("Photo not found.");
            }

            return Ok(photoUrl);
        }
        catch (Exception)
        {
            // Log the exception
            return StatusCode(500, "Unable to retrieve photo.");
        }
    }

    // GET: MyTrips
    public async Task<IActionResult> MyTrips()
    {
        var userId = GetCurrentUserId();

        // Pobierz wycieczki gdzie użytkownik jest właścicielem
        var ownedTrips = await _tripService.GetUserTripsAsync(userId);

        var ownedTripsViewModel = new List<TripViewModel>();
        foreach (var t in ownedTrips)
        {
            ownedTripsViewModel.Add(new TripViewModel
            {
                Id = t.Id,
                Name = t.Name,
                Status = t.Status,
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                IsPrivate = t.IsPrivate,
                DaysCount = (t.Days ?? Enumerable.Empty<Day>()).Where(d => d.Number.HasValue).Count(),
                GroupsCount = (t.Days ?? Enumerable.Empty<Day>()).Where(d => !d.Number.HasValue).Count(),
                ParticipantsCount = await _tripParticipantService.GetParticipantCountAsync(t.Id),
                IsOwner = true
            });
        }

        // Pobierz wycieczki gdzie użytkownik jest uczestnikiem
        var participatingTrips = await _tripParticipantService.GetUserParticipatingTripsAsync(userId);

        // Odfiltruj wycieczki, których użytkownik jest właścicielem
        var ownedTripIds = ownedTrips.Select(t => t.Id).ToHashSet();
        var filteredParticipatingTrips = participatingTrips.Where(tp => !ownedTripIds.Contains(tp.Trip.Id) && tp.Status == TripParticipantStatus.Accepted);

        var participatingTripsViewModel = new List<TripViewModel>();
        foreach (var tp in filteredParticipatingTrips)
        {
            participatingTripsViewModel.Add(new TripViewModel
            {
                Id = tp.Trip.Id,
                Name = tp.Trip.Name,
                Status = tp.Trip.Status,
                StartDate = tp.Trip.StartDate,
                EndDate = tp.Trip.EndDate,
                IsPrivate = tp.Trip.IsPrivate,
                DaysCount = (tp.Trip.Days ?? Enumerable.Empty<Day>()).Where(d => d.Number.HasValue).Count(),
                GroupsCount = (tp.Trip.Days ?? Enumerable.Empty<Day>()).Where(d => !d.Number.HasValue).Count(),
                ParticipantsCount = await _tripParticipantService.GetParticipantCountAsync(tp.Trip.Id),
                IsOwner = false
            });
        }

        // Pobierz oczekujące zaproszenia
        var pendingInvitations = await _tripParticipantService.GetPendingInvitationsAsync(userId);

        var pendingInvitationsViewModel = new List<TripViewModel>();
        foreach (var tp in pendingInvitations)
        {
            pendingInvitationsViewModel.Add(new TripViewModel
            {
                Id = tp.Trip.Id,
                Name = tp.Trip.Name,
                Status = tp.Trip.Status,
                StartDate = tp.Trip.StartDate,
                EndDate = tp.Trip.EndDate,
                IsPrivate = tp.Trip.IsPrivate,
                DaysCount = (tp.Trip.Days ?? Enumerable.Empty<Day>()).Where(d => d.Number.HasValue).Count(),
                GroupsCount = (tp.Trip.Days ?? Enumerable.Empty<Day>()).Where(d => !d.Number.HasValue).Count(),
                ParticipantsCount = await _tripParticipantService.GetParticipantCountAsync(tp.Trip.Id),
                IsOwner = false,
                UserParticipantStatus = TripParticipantStatus.Pending,
                ParticipantId = tp.Id // Dodane dla formularzy Accept/Decline
            });
        }

        var viewModel = new MyTripsViewModel
        {
            OwnedTrips = ownedTripsViewModel,
            ParticipatingTrips = participatingTripsViewModel,
            PendingInvitations = pendingInvitationsViewModel
        };

        return View(viewModel);
    }

    // GET: Trips/GetTripCountries/5
    public async Task<IActionResult> GetTripCountries(int id)
    {
        if (!await _tripParticipantService.UserHasAccessToTripAsync(id, GetCurrentUserId()))
        {
            return Forbid();
        }

        var countries = await _spotService.GetCountriesByTripAsync(id);
        var countryViewModels = countries.Select(c => new CountryViewModel
        {
            Code = c.Code,
            Name = c.Name,
            SpotsCount = c.Spots?.Count ?? 0
        }).ToList();

        return Ok(countryViewModels);
    }

    [HttpGet]
    public async Task<IActionResult> Checklist(int tripId, string source = "")
    {
        var trip = await _tripService.GetByIdWithParticipantsAsync(tripId);
        if (trip == null) return NotFound();

        if (source != "public" && !await _tripParticipantService.UserHasAccessToTripAsync(trip.Id, GetCurrentUserId()))
        {
            return Forbid();
        }

        var vm = new ChecklistPageViewModel
        {
            TripId = tripId,
            Checklist = trip.Checklist ?? new Checklist(),
            Participants = trip.Participants.Select(p => new ParticipantVm { Id = p.Id.ToString(), DisplayName = p.Person?.FirstName + " " + p.Person?.LastName }).ToList()
        };
        ViewBag.TripId = tripId;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignParticipant(int tripId, string itemTitle, string? participantId)
    {
        participantId = string.IsNullOrWhiteSpace(participantId) ? null : participantId;
        await _tripService.AssignParticipantToItemAsync(tripId, itemTitle, participantId);
        return RedirectToAction("Checklist", new { tripId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddChecklistItem(int tripId, string item)
    {
        if (string.IsNullOrWhiteSpace(item)) return RedirectToAction("Checklist", new { tripId });
        await _tripService.AddChecklistItemAsync(tripId, item);
        return RedirectToAction("Checklist", new { tripId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleChecklistItem(int tripId, string item)
    {
        await _tripService.ToggleChecklistItemAsync(tripId, item);
        return RedirectToAction("Checklist", new { tripId });
    }

    /// <summary>
    /// GET: show intermediate edit view for a checklist item.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> EditChecklistItem(int tripId, string item)
    {
        // Validate input
        if (string.IsNullOrEmpty(item))
            return BadRequest();

        // Optionally check that trip and item exist
        var trip = await _tripService.GetByIdAsync(tripId);
        if (trip == null) return NotFound();

        var model = new EditChecklistItemViewModel
        {
            TripId = tripId,
            OldItem = item,
            NewItem = item // prefill with current title
        };

        return View("EditChecklistItem", model);
    }

    /// <summary>
    /// POST: commit rename of checklist item.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditChecklistItem(EditChecklistItemViewModel vm)
    {
        if (!ModelState.IsValid)
            return View("EditChecklistItem", vm);

        try
        {
            await _tripService.RenameChecklistItemAsync(vm.TripId, vm.OldItem, vm.NewItem);
            return RedirectToAction("Checklist", new { tripId = vm.TripId });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            // show error to user on the same edit page
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("EditChecklistItem", vm);
        }
    }

    // POST via form (non-AJAX)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteChecklistItem(int tripId, string item)
    {
        if (string.IsNullOrWhiteSpace(item)) return BadRequest();

        await _tripService.RemoveChecklistItemAsync(tripId, item);
        return RedirectToAction("Checklist", new { tripId });
    }

    /// <summary>
    /// Mark all checklist items as completed
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllComplete(int tripId)
    {
        try
        {
            await _tripService.MarkAllChecklistItemsAsync(tripId, true);
            TempData["SuccessMessage"] = "All items have been marked as complete!";
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error marking items as complete: {ex.Message}";
        }

        return RedirectToAction("Checklist", new { tripId });
    }

    /// <summary>
    /// Mark all checklist items as incomplete
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllIncomplete(int tripId)
    {
        try
        {
            await _tripService.MarkAllChecklistItemsAsync(tripId, false);
            TempData["SuccessMessage"] = "All items have been marked as incomplete!";
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error marking items as incomplete: {ex.Message}";
        }

        return RedirectToAction("Checklist", new { tripId });
    }

    // GET: Trips/FlightList/5
    [HttpGet]
    public async Task<IActionResult> FlightList(int id)
    {
        var trip = await _tripService.GetByIdAsync(id);
        if (trip == null)
        {
            return NotFound();
        }

        if (!await _tripParticipantService.UserHasAccessToTripAsync(id, GetCurrentUserId()))
        {
            return Forbid();
        }

        // Pobierz loty z bazy danych
        var flightsForTrip = await _flightInfoService.GetByTripIdAsync(id);

        var viewModel = new FlightListViewModel
        {
            TripId = id,
            TripName = trip.Name,
            Flights = flightsForTrip.Select(f =>
            {
                var flightViewModel = new FlightInfoViewModel
                {
                    Id = f.Id,
                    TripId = f.TripId,
                    OriginAirportCode = f.OriginAirportCode ?? "N/A",
                    DestinationAirportCode = f.DestinationAirportCode ?? "N/A",
                    DepartureTime = f.DepartureTime,
                    ArrivalTime = f.ArrivalTime,
                    Duration = f.Duration,
                    Price = f.Price,
                    Currency = f.Currency,
                    Airline = f.Airline,
                    FlightNumbers = f.Segments
                        .Where(s => !string.IsNullOrEmpty(s.FullFlightNumber))
                        .Select(s => s.FullFlightNumber!)
                        .ToList(),
                    BookingReference = f.BookingReference,
                    Notes = f.Notes,
                    IsConfirmed = f.IsConfirmed,
                    AddedAt = f.AddedAt,
                    AddedByName = $"{f.AddedBy.FirstName} {f.AddedBy.LastName}",
                    Segments = f.Segments,
                    TotalConnectionTime = f.TotalConnectionTime,
                    PureFlightTime = f.PureFlightTime
                };

                // Oblicz informacje o przesiadkach
                flightViewModel.CalculateConnectionInfo();

                return flightViewModel;
            }).ToList()
        };

        return View(viewModel);
    }

    // POST: Save flight from search
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveFlightFromSearch(int tripId, [FromBody] SaveFlightRequestDto request)
    {
        try
        {
            var trip = await _tripService.GetByIdAsync(tripId);
            if (trip == null)
            {
                return Json(new { success = false, message = "Trip not found" });
            }

            if (!await _tripParticipantService.UserHasAccessToTripAsync(tripId, GetCurrentUserId()))
            {
                return Json(new { success = false, message = "Access denied" });
            }

            // Pobierz użytkownika
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            // Przekonwertuj DTO na encję FlightSegment
            var segments = request.Segments?.Select(s => new FlightSegment
            {
                OriginAirportCode = s.OriginAirportCode,
                DestinationAirportCode = s.DestinationAirportCode,
                DepartureTime = s.DepartureTime ?? DateTime.MinValue,
                ArrivalTime = s.ArrivalTime ?? DateTime.MinValue,
                Duration = s.Duration ?? TimeSpan.Zero,
                CarrierCode = s.CarrierCode,
                FlightNumber = s.FlightNumber
            }).ToList() ?? new List<FlightSegment>();

            // Oblicz całkowity czas lotu z przesiadkami
            TimeSpan totalDuration = request.Duration;
            if (segments.Count > 1)
            {
                var firstSegment = segments.First();
                var lastSegment = segments.Last();

                if (firstSegment.DepartureTime.HasValue && lastSegment.ArrivalTime.HasValue)
                {
                    totalDuration = lastSegment.ArrivalTime.Value - firstSegment.DepartureTime.Value;
                }
            }

            // Zapisz lot do bazy
            var flightInfo = new FlightInfo
            {
                TripId = tripId,
                OriginAirportCode = request.OriginAirportCode,
                DestinationAirportCode = request.DestinationAirportCode,
                DepartureTime = request.DepartureTime,
                ArrivalTime = request.ArrivalTime,
                Duration = totalDuration, // Używamy obliczonego czasu
                Price = request.TotalPrice,
                Currency = request.Currency,
                Airline = request.CarrierCode,
                FlightNumbers = segments
                    .Where(s => !string.IsNullOrEmpty(s.FullFlightNumber))
                    .Select(s => s.FullFlightNumber!)
                    .ToList(),
                PersonId = GetCurrentUserId(),
                AddedBy = user,
                Segments = segments,
                Trip = trip
            };

            await _flightInfoService.AddAsync(flightInfo);

            return Json(new
            {
                success = true,
                message = "Flight saved to your trip!",
                redirectUrl = Url.Action("FlightList", new { id = tripId })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving flight from search");
            return Json(new { success = false, message = "Error saving flight" });
        }
    }

    // GET: Trips/AddFlight/5
    [HttpGet]
    public async Task<IActionResult> AddFlight(int id)
    {
        var trip = await _tripService.GetByIdAsync(id);
        if (trip == null)
        {
            return NotFound();
        }

        if (!await _tripParticipantService.UserHasAccessToTripAsync(id, GetCurrentUserId()))
        {
            return Forbid();
        }

        var viewModel = new AddFlightViewModel
        {
            TripId = id,
            DepartureTime = DateTime.Now.AddDays(7),
            ArrivalTime = DateTime.Now.AddDays(7).AddHours(2),
            Currency = trip.CurrencyCode.ToString()
        };

        return View(viewModel);
    }

    // POST: Trips/AddFlight
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddFlight(AddFlightViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        try
        {
            var trip = await _tripService.GetByIdAsync(viewModel.TripId);
            if (trip == null)
            {
                return NotFound();
            }

            if (!await _tripParticipantService.UserHasAccessToTripAsync(viewModel.TripId, GetCurrentUserId()))
            {
                return Forbid();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            // Przetwórz segmenty
            var segments = viewModel.Segments?.Select(s => new FlightSegment
            {
                OriginAirportCode = s.OriginAirportCode,
                DestinationAirportCode = s.DestinationAirportCode,
                DepartureTime = s.DepartureTime,
                ArrivalTime = s.ArrivalTime,
                CarrierCode = s.CarrierCode,
                FlightNumber = s.FlightNumber,
                Duration = s.ArrivalTime - s.DepartureTime
            }).ToList() ?? new List<FlightSegment>();

            // Oblicz całkowity czas lotu
            var totalDuration = TimeSpan.Zero;
            if (segments.Count > 0)
            {
                var firstSegment = segments.First();
                var lastSegment = segments.Last();
                totalDuration = lastSegment.ArrivalTime!.Value - firstSegment.DepartureTime!.Value;
            }

            // Przetwórz numery lotów z segmentów
            var flightNumbers = segments
                .Where(s => !string.IsNullOrEmpty(s.CarrierCode) && !string.IsNullOrEmpty(s.FlightNumber))
                .Select(s => $"{s.CarrierCode}{s.FlightNumber}")
                .ToList();

            // Zapisz lot do bazy
            var flightInfo = new FlightInfo
            {
                TripId = viewModel.TripId,
                OriginAirportCode = segments.FirstOrDefault()?.OriginAirportCode ?? viewModel.OriginAirportCode,
                DestinationAirportCode = segments.LastOrDefault()?.DestinationAirportCode ?? viewModel.DestinationAirportCode,
                DepartureTime = segments.FirstOrDefault()?.DepartureTime ?? viewModel.DepartureTime,
                ArrivalTime = segments.LastOrDefault()?.ArrivalTime ?? viewModel.ArrivalTime,
                Duration = totalDuration,
                Price = viewModel.Price,
                Currency = viewModel.Currency,
                Airline = viewModel.Airline,
                FlightNumbers = flightNumbers,
                BookingReference = viewModel.BookingReference,
                Notes = viewModel.Notes,
                IsConfirmed = viewModel.IsConfirmed,
                PersonId = GetCurrentUserId(),
                AddedBy = user,
                Trip = trip,
                Segments = segments
            };

            await _flightInfoService.AddAsync(flightInfo);

            TempData["SuccessMessage"] = "Flight added successfully!";
            return RedirectToAction("FlightList", new { id = viewModel.TripId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding flight manually");
            ModelState.AddModelError("", "An error occurred while adding the flight.");
            return View(viewModel);
        }
    }

    // GET: Trips/EditFlight/5
    [HttpGet]
    public async Task<IActionResult> EditFlight(int tripId, int flightId)
    {
        var trip = await _tripService.GetByIdAsync(tripId);
        if (trip == null)
        {
            return NotFound();
        }

        if (!await _tripParticipantService.UserHasAccessToTripAsync(tripId, GetCurrentUserId()))
        {
            return Forbid();
        }

        // Sprawdź czy użytkownik może edytować ten lot
        if (!await _flightInfoService.UserCanModifyFlightAsync(flightId, GetCurrentUserId()))
        {
            return Forbid();
        }

        var flight = await _flightInfoService.GetByIdAsync(flightId);
        if (flight == null)
        {
            return NotFound();
        }

        var viewModel = new EditFlightViewModel
        {
            Id = flight.Id,
            TripId = tripId,
            OriginAirportCode = flight.OriginAirportCode ?? "",
            DestinationAirportCode = flight.DestinationAirportCode ?? "",
            DepartureTime = flight.DepartureTime,
            ArrivalTime = flight.ArrivalTime,
            Price = flight.Price,
            Currency = flight.Currency,
            Airline = flight.Airline,
            FlightNumbers = string.Join(", ", flight.FlightNumbers),
            BookingReference = flight.BookingReference,
            Notes = flight.Notes,
            IsConfirmed = flight.IsConfirmed,
            Segments = flight.Segments.Select(s => new FlightSegmentViewModel
            {
                OriginAirportCode = s.OriginAirportCode ?? "",
                DestinationAirportCode = s.DestinationAirportCode ?? "",
                DepartureTime = s.DepartureTime ?? DateTime.Now,
                ArrivalTime = s.ArrivalTime ?? DateTime.Now.AddHours(2),
                CarrierCode = s.CarrierCode,
                FlightNumber = s.FlightNumber
            }).ToList(),
            NumberOfSegments = flight.Segments.Count
        };

        return View(viewModel);
    }

    // POST: Trips/EditFlight/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditFlight(int tripId, EditFlightViewModel viewModel)
    {
        if (tripId != viewModel.TripId)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        try
        {
            var trip = await _tripService.GetByIdAsync(tripId);
            if (trip == null)
            {
                return NotFound();
            }

            if (!await _tripParticipantService.UserHasAccessToTripAsync(tripId, GetCurrentUserId()))
            {
                return Forbid();
            }

            // Sprawdź czy użytkownik może edytować ten lot
            if (!await _flightInfoService.UserCanModifyFlightAsync(viewModel.Id, GetCurrentUserId()))
            {
                return Forbid();
            }

            var existingFlight = await _flightInfoService.GetByIdAsync(viewModel.Id);
            if (existingFlight == null)
            {
                return NotFound();
            }

            // Przetwórz segmenty
            var segments = viewModel.Segments?.Select(s => new FlightSegment
            {
                OriginAirportCode = s.OriginAirportCode,
                DestinationAirportCode = s.DestinationAirportCode,
                DepartureTime = s.DepartureTime,
                ArrivalTime = s.ArrivalTime,
                CarrierCode = s.CarrierCode,
                FlightNumber = s.FlightNumber,
                Duration = s.ArrivalTime - s.DepartureTime
            }).ToList() ?? new List<FlightSegment>();

            // Oblicz całkowity czas lotu
            var totalDuration = TimeSpan.Zero;
            if (segments.Count > 0)
            {
                var firstSegment = segments.First();
                var lastSegment = segments.Last();
                totalDuration = lastSegment.ArrivalTime!.Value - firstSegment.DepartureTime!.Value;
            }

            // Przetwórz numery lotów z segmentów
            var flightNumbers = segments
                .Where(s => !string.IsNullOrEmpty(s.CarrierCode) && !string.IsNullOrEmpty(s.FlightNumber))
                .Select(s => $"{s.CarrierCode}{s.FlightNumber}")
                .ToList();

            // Aktualizuj lot
            existingFlight.OriginAirportCode = segments.FirstOrDefault()?.OriginAirportCode ?? viewModel.OriginAirportCode;
            existingFlight.DestinationAirportCode = segments.LastOrDefault()?.DestinationAirportCode ?? viewModel.DestinationAirportCode;
            existingFlight.DepartureTime = segments.FirstOrDefault()?.DepartureTime ?? viewModel.DepartureTime;
            existingFlight.ArrivalTime = segments.LastOrDefault()?.ArrivalTime ?? viewModel.ArrivalTime;
            existingFlight.Duration = totalDuration;
            existingFlight.Price = viewModel.Price;
            existingFlight.Currency = viewModel.Currency;
            existingFlight.Airline = viewModel.Airline;
            existingFlight.FlightNumbers = flightNumbers;
            existingFlight.BookingReference = viewModel.BookingReference;
            existingFlight.Notes = viewModel.Notes;
            existingFlight.IsConfirmed = viewModel.IsConfirmed;
            existingFlight.Segments = segments;

            await _flightInfoService.UpdateAsync(existingFlight);

            TempData["SuccessMessage"] = "Flight updated successfully!";
            return RedirectToAction("FlightList", new { id = tripId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating flight");
            ModelState.AddModelError("", "An error occurred while updating the flight.");
            return View(viewModel);
        }
    }

    // POST: Trips/DeleteFlight/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFlight(int id, int flightId)
    {
        try
        {
            var trip = await _tripService.GetByIdAsync(id);
            if (trip == null)
            {
                return NotFound();
            }

            if (!await _tripParticipantService.UserHasAccessToTripAsync(id, GetCurrentUserId()))
            {
                return Forbid();
            }

            // Sprawdź czy użytkownik może usunąć ten lot
            if (!await _flightInfoService.UserCanModifyFlightAsync(flightId, GetCurrentUserId()))
            {
                return Forbid();
            }

            await _flightInfoService.DeleteAsync(flightId);

            TempData["SuccessMessage"] = "Flight removed successfully!";
            return RedirectToAction("FlightList", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting flight");
            TempData["ErrorMessage"] = "Error deleting flight.";
            return RedirectToAction("FlightList", new { id });
        }
    }

    // POST: Trips/ToggleFlightConfirmation/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleFlightConfirmation(int id, int flightId)
    {
        try
        {
            var trip = await _tripService.GetByIdAsync(id);
            if (trip == null)
            {
                return Json(new { success = false, message = "Trip not found" });
            }

            if (!await _tripParticipantService.UserHasAccessToTripAsync(id, GetCurrentUserId()))
            {
                return Json(new { success = false, message = "Access denied" });
            }

            // Sprawdź czy użytkownik może modyfikować ten lot
            if (!await _flightInfoService.UserCanModifyFlightAsync(flightId, GetCurrentUserId()))
            {
                return Json(new { success = false, message = "Access denied" });
            }

            var flight = await _flightInfoService.GetByIdAsync(flightId);
            if (flight == null)
            {
                return Json(new { success = false, message = "Flight not found" });
            }

            await _flightInfoService.ToggleConfirmationAsync(flightId, !flight.IsConfirmed);

            return Json(new { success = true, isConfirmed = !flight.IsConfirmed });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling flight confirmation");
            return Json(new { success = false, message = "Error updating flight status" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetTripParticipants(int id)
    {
        if (!await _tripParticipantService.UserHasAccessToTripAsync(id, GetCurrentUserId()))
        {
            return Forbid();
        }

        var participants = await _tripParticipantService.GetTripParticipantsAsync(id);

        var participantViewModels = participants.Select(p => new
        {
            id = p.Id,
            personId = p.PersonId,
            firstName = p.Person?.FirstName ?? "Unknown",
            lastName = p.Person?.LastName ?? "User",
            email = p.Person?.Email ?? "No email",
            status = p.Status.ToString(),
            isOwner = p.Status == TripParticipantStatus.Owner,
            joinedAt = p.JoinedAt.ToString("yyyy-MM-ddTHH:mm:ss")
        }).ToList();

        return Ok(participantViewModels);
    }

    public async Task<IActionResult> GetDistance(int id, double lat, double lng)
    {
        double distance = await _tripService.GetDistance(id, lat, lng);
        return Ok(distance);
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