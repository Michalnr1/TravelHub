using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelHub.Domain.DTOs;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Services;
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
    private readonly ITripParticipantService _tripParticipantService;
    private readonly ITransportService _transportService;
    private readonly ISpotService _spotService;
    private readonly IActivityService _activityService;
    private readonly ICategoryService _categoryService;
    private readonly IAccommodationService _accommodationService;
    private readonly IExpenseService _expenseService;
    private readonly IFlightService _flightService;
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
                PaidByName = e.PaidBy != null ? $"{e.PaidBy.FirstName} {e.PaidBy.LastName}" : "Unknown",
                TransferredToName = e.TransferredTo == null ? null : e.TransferredTo.FirstName + " " + e.TransferredTo.LastName,
                CategoryName = e.Category?.Name,
                CurrencyName = e.ExchangeRate?.Name ?? trip.CurrencyCode.GetDisplayName(),
                CurrencyCode = e.ExchangeRate?.CurrencyCodeKey ?? trip.CurrencyCode,
                ExchangeRateValue = e.ExchangeRate?.ExchangeRateValue ?? 1m,
                ConvertedValue = calculation?.ConvertedValue ?? e.Value
            };
        }).ToList();

        var viewModel = new TripDetailViewModel
        {
            Id = trip.Id,
            Name = trip.Name,
            Status = trip.Status,
            StartDate = trip.StartDate,
            EndDate = trip.EndDate,
            IsPrivate = trip.IsPrivate,
            CurrencyCode = trip.CurrencyCode,
            TotalExpenses = expensesSummary.TotalExpensesInTripCurrency,
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

    public async Task<IActionResult> MapView(int id)
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

        (double lat, double lng) = await _tripService.GetMedianCoords(id);

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
                    IsPrivate = viewModel.IsPrivate
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
        (double lat, double lng) = await _tripService.GetMedianCoords(id);
        AirportDto? airport = await _flightService.GetAirportByCoords(lat, lng);
        Person? user = await _userManager.GetUserAsync(User);
        if (user != null && user.DefaultAirportCode != null)
        {
            ViewData["FromAirportCode"] = user.DefaultAirportCode;
        } else
        {
            ViewData["FromAirportCode"] = "";
        }
        if (airport != null)
        {
            ViewData["ToAirportCode"] = airport.AirportCode;
        }
        else
        {
            ViewData["AirportCode"] = "";
        }
        return View();
    }

    public async Task<IActionResult> Flights(string from, string to, string date, int? adults, int? children, int? seatedInfants, int? heldInfants)
    {
        if (from == null || to == null || date == null) { return BadRequest(); }
        bool dateValid = DateTime.TryParseExact(date, "yyyy-MM-dd", null, 0, out DateTime result);
        if (!dateValid) { return BadRequest(); }
        try
        {
            var flights = await _flightService.GetFlights(from, to, result, null, adults, children, seatedInfants, heldInfants);
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
        var filteredParticipatingTrips = participatingTrips.Where(tp => !ownedTripIds.Contains(tp.Trip.Id));

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