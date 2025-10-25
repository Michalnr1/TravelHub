using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Infrastructure.Services;
using TravelHub.Web.ViewModels.Accommodations;

namespace TravelHub.Web.Controllers
{
    [Authorize]
    public class AccommodationsController : Controller
    {
        private readonly IAccommodationService _accommodationService;
        private readonly ICategoryService _categoryService;
        private readonly ITripService _tripService;
        private readonly IDayService _dayService;
        private readonly ISpotService _spotService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccommodationsController> _logger;

        public AccommodationsController(
            IAccommodationService accommodationService,
            ICategoryService categoryService,
            ITripService tripService,
            IDayService dayService,
            ISpotService spotService,
            IConfiguration configuration,
            ILogger<AccommodationsController> logger)
        {
            _accommodationService = accommodationService;
            _categoryService = categoryService;
            _tripService = tripService;
            _dayService = dayService;
            _spotService = spotService;
            _configuration = configuration;
            _logger = logger;
        }

        // GET: Accommodations
        public async Task<IActionResult> Index()
        {
            var accommodations = await _accommodationService.GetAllAsync();
            var viewModel = accommodations.Select(a => new AccommodationViewModel
            {
                Id = a.Id,
                Name = a.Name,
                Description = a.Description,
                Cost = a.Cost,
                CategoryName = a.Category?.Name,
                DayName = a.Day?.Name,
                CheckIn = a.CheckIn,
                CheckOut = a.CheckOut
            }).ToList();

            return View(viewModel);
        }

        // GET: Accommodations/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var accommodation = await _accommodationService.GetByIdWithDetailsAsync(id.Value);
            if (accommodation == null)
            {
                return NotFound();
            }

            var viewModel = new AccommodationDetailsViewModel
            {
                Id = accommodation.Id,
                Name = accommodation.Name,
                Description = accommodation.Description,
                Duration = accommodation.Duration,
                Order = accommodation.Order,
                CategoryName = accommodation.Category?.Name,
                DayName = accommodation.Day?.Name,
                Longitude = accommodation.Longitude,
                Latitude = accommodation.Latitude,
                Cost = accommodation.Cost,
                CheckIn = accommodation.CheckIn,
                CheckOut = accommodation.CheckOut,
                CheckInTime = accommodation.CheckInTime,
                CheckOutTime = accommodation.CheckOutTime,
                TripId = accommodation.TripId,
                TripName = accommodation.Trip?.Name
            };

            return View(viewModel);
        }

        // GET: Accommodations/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = await CreateAccommodationCreateEditViewModel();
            return View(viewModel);
        }

        // POST: Accommodations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AccommodationCreateEditViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var accommodation = new Accommodation
                {
                    Name = viewModel.Name,
                    Description = viewModel.Description,
                    Duration = viewModel.Duration,
                    Order = viewModel.Order,
                    CategoryId = viewModel.CategoryId,
                    TripId = viewModel.TripId,
                    DayId = viewModel.DayId,
                    Longitude = viewModel.Longitude,
                    Latitude = viewModel.Latitude,
                    Cost = viewModel.Cost,
                    CheckIn = viewModel.CheckIn,
                    CheckOut = viewModel.CheckOut,
                    CheckInTime = viewModel.CheckInTime,
                    CheckOutTime = viewModel.CheckOutTime
                };

                await _accommodationService.AddAsync(accommodation);
                return RedirectToAction(nameof(Index));
            }

            await PopulateSelectLists(viewModel);
            return View(viewModel);
        }

        // GET: Accommodations/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var accommodation = await _accommodationService.GetByIdAsync(id.Value);
            if (accommodation == null)
            {
                return NotFound();
            }

            var viewModel = await CreateAccommodationCreateEditViewModel(accommodation);
            return View(viewModel);
        }

        // POST: Accommodations/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AccommodationCreateEditViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingAccommodation = await _accommodationService.GetByIdAsync(id);
                    if (existingAccommodation == null)
                    {
                        return NotFound();
                    }

                    // Update properties
                    existingAccommodation.Name = viewModel.Name;
                    existingAccommodation.Description = viewModel.Description;
                    existingAccommodation.Duration = viewModel.Duration;
                    existingAccommodation.Order = viewModel.Order;
                    existingAccommodation.CategoryId = viewModel.CategoryId;
                    existingAccommodation.DayId = viewModel.DayId;
                    existingAccommodation.Longitude = viewModel.Longitude;
                    existingAccommodation.Latitude = viewModel.Latitude;
                    existingAccommodation.Cost = viewModel.Cost;
                    existingAccommodation.CheckIn = viewModel.CheckIn;
                    existingAccommodation.CheckOut = viewModel.CheckOut;
                    existingAccommodation.CheckInTime = viewModel.CheckInTime;
                    existingAccommodation.CheckOutTime = viewModel.CheckOutTime;

                    await _accommodationService.UpdateAsync(existingAccommodation);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await AccommodationExists(viewModel.Id))
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

        // GET: Accommodations/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var accommodation = await _accommodationService.GetByIdAsync(id.Value);
            if (accommodation == null)
            {
                return NotFound();
            }

            var viewModel = new AccommodationDetailsViewModel
            {
                Id = accommodation.Id,
                Name = accommodation.Name,
                Description = accommodation.Description,
                Cost = accommodation.Cost,
                CheckIn = accommodation.CheckIn,
                CheckOut = accommodation.CheckOut
            };

            return View(viewModel);
        }

        // POST: Accommodations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _accommodationService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        // GET: Accommodations/AddToTrip/5
        public async Task<IActionResult> AddToTrip(int tripId)
        {
            var trip = await _tripService.GetByIdAsync(tripId);
            if (trip == null)
            {
                return NotFound();
            }

            var viewModel = new AccommodationCreateEditViewModel
            {
                TripId = tripId,
                Order = 0, // Order nie jest edytowalny przez użytkownika
                Duration = 0, // Duration nie jest istotne dla zakwaterowania
                CheckIn = trip.StartDate,
                CheckOut = trip.StartDate.AddDays(1),
                CheckInTime = 14.0m,
                CheckOutTime = 10.0m
            };

            await PopulateSelectListsForTrip(viewModel, tripId);

            ViewData["TripName"] = trip.Name;
            ViewData["ReturnUrl"] = Url.Action("Details", "Trips", new { id = tripId });
            ViewData["GoogleApiKey"] = _configuration["ApiKeys:GoogleApiKey"];
            ViewData["MinDate"] = trip.StartDate.ToString("yyyy-MM-dd");
            ViewData["MaxDate"] = trip.EndDate.ToString("yyyy-MM-dd");

            (double lat, double lng) = await GetMedianCoords(tripId);

            ViewData["Latitude"] = lat;
            ViewData["Longitude"] = lng;

            return View("AddToTrip", viewModel);
        }

        // POST: Accommodations/AddToTrip/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToTrip(int tripId, AccommodationCreateEditViewModel viewModel)
        {
            if (tripId != viewModel.TripId)
            {
                return NotFound();
            }

            var trip = await _tripService.GetByIdAsync(tripId);
            if (trip == null)
            {
                return NotFound();
            }

            // Walidacja dat w zakresie podróży
            if (viewModel.CheckIn < trip.StartDate || viewModel.CheckIn > trip.EndDate)
            {
                ModelState.AddModelError("CheckIn", $"Check-in date must be between {trip.StartDate:yyyy-MM-dd} and {trip.EndDate:yyyy-MM-dd}");
            }

            if (viewModel.CheckOut < trip.StartDate || viewModel.CheckOut > trip.EndDate)
            {
                ModelState.AddModelError("CheckOut", $"Check-out date must be between {trip.StartDate:yyyy-MM-dd} and {trip.EndDate:yyyy-MM-dd}");
            }

            if (viewModel.CheckOut <= viewModel.CheckIn)
            {
                ModelState.AddModelError("CheckOut", "Check-out date must be after check-in date");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Automatyczne przypisanie dnia na podstawie daty check-in
                    var dayId = await FindDayForDate(tripId, viewModel.CheckIn);

                    var accommodation = new Accommodation
                    {
                        Name = viewModel.Name,
                        Description = viewModel.Description,
                        Duration = 0, // Duration nie jest istotne dla zakwaterowania
                        Order = 0, // Order nie jest edytowalny
                        CategoryId = viewModel.CategoryId,
                        TripId = viewModel.TripId,
                        DayId = dayId, // Automatyczne przypisanie
                        Longitude = viewModel.Longitude,
                        Latitude = viewModel.Latitude,
                        Cost = viewModel.Cost,
                        CheckIn = viewModel.CheckIn,
                        CheckOut = viewModel.CheckOut,
                        CheckInTime = viewModel.CheckInTime,
                        CheckOutTime = viewModel.CheckOutTime
                    };

                    await _accommodationService.AddAsync(accommodation);

                    TempData["SuccessMessage"] = "Accommodation added successfully!";
                    return RedirectToAction("Details", "Trips", new { id = tripId });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error adding accommodation to trip");
                    ModelState.AddModelError("", "An error occurred while adding the accommodation.");
                }
            }

            await PopulateSelectListsForTrip(viewModel, tripId);
            ViewData["TripName"] = trip.Name;
            ViewData["ReturnUrl"] = Url.Action("Details", "Trips", new { id = tripId });
            ViewData["GoogleApiKey"] = _configuration["ApiKeys:GoogleApiKey"];
            ViewData["MinDate"] = trip.StartDate.ToString("yyyy-MM-dd");
            ViewData["MaxDate"] = trip.EndDate.ToString("yyyy-MM-dd");

            return View("AddToTrip", viewModel);
        }

        private async Task PopulateSelectListsForTrip(AccommodationCreateEditViewModel viewModel, int tripId)
        {
            // Categories
            var categories = await _categoryService.GetAllAsync();
            viewModel.Categories = categories.Select(c => new CategorySelectItem
            {
                Id = c.Id,
                Name = c.Name
            }).ToList();

            // Days - only for this trip (do wyświetlania informacji)
            var days = await _dayService.GetAllAsync();
            viewModel.Days = days.Where(d => d.TripId == tripId && d.Number.HasValue)
                .Select(d => new DaySelectItem
                {
                    Id = d.Id,
                    DisplayName = d.Name!
                }).ToList();
        }

        private async Task<int?> FindDayForDate(int tripId, DateTime date)
        {
            var days = await _dayService.GetAllAsync();
            var day = days.FirstOrDefault(d => d.TripId == tripId && d.Date.Date == date.Date);
            return day?.Id;
        }

        private async Task<(double lat, double lng)> GetMedianCoords(int tripId)
        {
            // Pobierz istniejące zakwaterowania i miejsca, aby ustalić medianę koordynatów
            var accommodations = await _accommodationService.GetAccommodationByTripAsync(tripId);
            var spots = await _spotService.GetSpotsByTripAsync(tripId); // Zakładając, że masz taki serwis

            var allCoords = accommodations.Select(a => (a.Latitude, a.Longitude))
                                .Concat(spots.Select(s => (s.Latitude, s.Longitude)))
                                .ToList();

            if (allCoords.Any())
            {
                var medianLat = allCoords.Select(c => c.Latitude).Average();
                var medianLng = allCoords.Select(c => c.Longitude).Average();
                return (medianLat, medianLng);
            }

            // Domyślne koordynaty (Warszawa)
            return (52.2297, 21.0122);
        }

        private async Task<bool> AccommodationExists(int id)
        {
            var accommodation = await _accommodationService.GetByIdAsync(id);
            return accommodation != null;
        }

        private async Task<AccommodationCreateEditViewModel> CreateAccommodationCreateEditViewModel(Accommodation? accommodation = null)
        {
            var viewModel = new AccommodationCreateEditViewModel();

            if (accommodation != null)
            {
                viewModel.Id = accommodation.Id;
                viewModel.Name = accommodation.Name;
                viewModel.Description = accommodation.Description;
                viewModel.Duration = accommodation.Duration;
                viewModel.Order = accommodation.Order;
                viewModel.CategoryId = accommodation.CategoryId;
                viewModel.TripId = accommodation.TripId;
                viewModel.DayId = accommodation.DayId;
                viewModel.Longitude = accommodation.Longitude;
                viewModel.Latitude = accommodation.Latitude;
                viewModel.Cost = accommodation.Cost;
                viewModel.CheckIn = accommodation.CheckIn;
                viewModel.CheckOut = accommodation.CheckOut;
                viewModel.CheckInTime = accommodation.CheckInTime;
                viewModel.CheckOutTime = accommodation.CheckOutTime;
            }

            await PopulateSelectLists(viewModel);
            return viewModel;
        }

        private async Task PopulateSelectLists(AccommodationCreateEditViewModel viewModel)
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

            // Days - filter by trip if TripId is set
            var days = await _dayService.GetAllAsync();
            var filteredDays = days.AsQueryable();

            if (viewModel.TripId > 0)
            {
                filteredDays = filteredDays.Where(d => d.TripId == viewModel.TripId);
            }

            viewModel.Days = filteredDays
                .Select(d => new DaySelectItem
                {
                    Id = d.Id,
                    DisplayName = d.Name!
                })
                .ToList();
        }
    }
}