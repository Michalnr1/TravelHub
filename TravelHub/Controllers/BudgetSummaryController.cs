using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TravelHub.Domain.DTOs;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Web.ViewModels.BudgetSummary;

namespace TravelHub.Web.Controllers;

[Authorize]
public class BudgetSummaryController : Controller
{
    private readonly IExpenseService _expenseService;
    private readonly ITripService _tripService;
    private readonly ICategoryService _categoryService;
    private readonly ITripParticipantService _tripParticipantService;

    public BudgetSummaryController(
        IExpenseService expenseService,
        ITripService tripService,
        ICategoryService categoryService,
        ITripParticipantService tripParticipantService)
    {
        _expenseService = expenseService;
        _tripService = tripService;
        _categoryService = categoryService;
        _tripParticipantService = tripParticipantService;
    }

    // GET: BudgetSummary/Index/5
    public async Task<IActionResult> Index(int tripId, string? personId, int? categoryId, bool includeTransfers = true, bool includeEstimated = true)
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

        var filter = new BudgetFilterDto
        {
            TripId = tripId,
            PersonId = personId,
            CategoryId = categoryId,
            IncludeTransfers = includeTransfers,
            IncludeEstimated = includeEstimated
        };

        var budgetSummary = await _expenseService.GetBudgetSummaryAsync(filter);
        var viewModel = BudgetSummaryViewModel.FromDto(budgetSummary);

        viewModel.FilterByCategoryId = categoryId;

        // Dodaj listy do filtrów
        await PopulateFilterLists(viewModel, tripId);

        return View(viewModel);
    }

    // POST: BudgetSummary/Index (dla formularza filtrowania)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(BudgetFilterViewModel filterModel)
    {
        if (ModelState.IsValid)
        {
            var trip = await _tripService.GetByIdAsync(filterModel.TripId);
            if (trip == null)
                return NotFound();

            var filter = new BudgetFilterDto
            {
                TripId = filterModel.TripId,
                PersonId = filterModel.PersonId,
                CategoryId = filterModel.CategoryId,
                IncludeTransfers = filterModel.IncludeTransfers,
                IncludeEstimated = filterModel.IncludeEstimated
            };

            var budgetSummary = await _expenseService.GetBudgetSummaryAsync(filter);
            var viewModel = BudgetSummaryViewModel.FromDto(budgetSummary);

            viewModel.FilterByCategoryId = filterModel.CategoryId;

            await PopulateFilterLists(viewModel, filterModel.TripId);

            viewModel.IncludeTransfers = filterModel.IncludeTransfers;
            viewModel.IncludeEstimated = filterModel.IncludeEstimated;

            return View(viewModel);
        }

        return RedirectToAction(nameof(Index), new
        {
            tripId = filterModel.TripId,
            personId = filterModel.PersonId,
            categoryId = filterModel.CategoryId,
            includeTransfers = filterModel.IncludeTransfers,
            includeEstimated = filterModel.IncludeEstimated
        });
    }

    // GET: BudgetSummary/PublicIndex/5
    public async Task<IActionResult> PublicIndex(int tripId, int? categoryId, bool includeEstimated = true)
    {
        var trip = await _tripService.GetByIdAsync(tripId);
        if (trip == null)
        {
            return NotFound();
        }

        // Sprawdź czy wycieczka jest publiczna
        if (trip.IsPrivate)
        {
            return Forbid();
        }

        var filter = new BudgetFilterDto
        {
            TripId = tripId,
            CategoryId = categoryId,
            IncludeTransfers = false, // Zawsze false dla publicznego widoku
            IncludeEstimated = includeEstimated
        };

        var budgetSummary = await _expenseService.GetBudgetSummaryAsync(filter);
        var viewModel = BudgetSummaryViewModel.FromDto(budgetSummary);

        // Ustaw właściwe FilterByCategoryId dla widoku (0 zamiast null)
        viewModel.FilterByCategoryId = categoryId;

        // Dodaj tylko kategorie do filtrów (bez osób)
        await PopulatePublicFilterLists(viewModel, tripId);

        viewModel.IncludeEstimated = includeEstimated;
        viewModel.IncludeTransfers = false; // Zawsze false

        return View(viewModel);
    }

    // POST: BudgetSummary/PublicIndex (dla formularza filtrowania)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PublicIndex(PublicBudgetFilterViewModel filterModel)
    {
        if (ModelState.IsValid)
        {
            var trip = await _tripService.GetByIdAsync(filterModel.TripId);
            if (trip == null || trip.IsPrivate)
                return Forbid();

            var filter = new BudgetFilterDto
            {
                TripId = filterModel.TripId,
                CategoryId = filterModel.CategoryId,
                IncludeTransfers = false, // Zawsze false
                IncludeEstimated = filterModel.IncludeEstimated
            };

            var budgetSummary = await _expenseService.GetBudgetSummaryAsync(filter);
            var viewModel = BudgetSummaryViewModel.FromDto(budgetSummary);

            // Ustaw właściwe FilterByCategoryId dla widoku (0 zamiast null)
            viewModel.FilterByCategoryId = filterModel.CategoryId;

            await PopulatePublicFilterLists(viewModel, filterModel.TripId);

            viewModel.IncludeEstimated = filterModel.IncludeEstimated;
            viewModel.IncludeTransfers = false;

            return View(viewModel);
        }

        return RedirectToAction(nameof(PublicIndex), new
        {
            tripId = filterModel.TripId,
            categoryId = filterModel.CategoryId,
            includeEstimated = filterModel.IncludeEstimated
        });
    }

    private async Task PopulatePublicFilterLists(BudgetSummaryViewModel viewModel, int tripId)
    {
        // Pobierz tylko kategorie (bez osób)
        var categories = await _categoryService.GetAllCategoriesByTripAsync(viewModel.TripId);

        // Dodaj kategorie do ViewModel
        viewModel.AvailableCategories = categories.Select(c => new CategoryFilterItem
        {
            Id = c.Id,
            Name = c.Name,
            Color = c.Color
        }).ToList();

        //viewModel.AvailableCategoriesSelectList = categories.Select(c => new SelectListItem
        //{
        //    Value = c.Id.ToString(),
        //    Text = c.Name,
        //    Selected = c.Id == viewModel.FilterByCategoryId
        //}).ToList();

        // Tworzymy SelectList z opcją "Uncategorized"
        var categorySelectList = categories.Select(c => new SelectListItem
        {
            Value = c.Id.ToString(),
            Text = c.Name,
            Selected = c.Id == viewModel.FilterByCategoryId
        }).ToList();

        // Dodaj opcję "Uncategorized" (ID = 0 lub null)
        categorySelectList.Insert(0, new SelectListItem
        {
            Value = "0", // Używamy 0 dla uncategorized
            Text = "Uncategorized",
            Selected = viewModel.FilterByCategoryId == 0
        });

        viewModel.AvailableCategoriesSelectList = categorySelectList;

        // Wyczyść listy osób
        viewModel.AvailablePeople = new List<PersonFilterItem>();
        viewModel.AvailablePeopleSelectList = new List<SelectListItem>();
        viewModel.PersonSummaries = new List<BudgetPersonSummaryViewModel>();
    }

    private async Task PopulateFilterLists(BudgetSummaryViewModel viewModel, int tripId)
    {
        // Pobierz uczestników wycieczki
        var participants = await _tripService.GetAllTripParticipantsAsync(tripId);

        // Dodaj uczestników do ViewModel
        viewModel.AvailablePeople = participants.Select(p => new PersonFilterItem
        {
            Id = p.Id,
            FullName = $"{p.FirstName} {p.LastName}"
        }).ToList();

        viewModel.AvailablePeopleSelectList = participants.Select(p => new SelectListItem
        {
            Value = p.Id,
            Text = $"{p.FirstName} {p.LastName}",
            Selected = p.Id == viewModel.FilterByPersonId
        }).ToList();

        // Pobierz kategorie
        var categories = await _categoryService.GetAllCategoriesByTripAsync(viewModel.TripId);

        // Dodaj kategorie do ViewModel
        viewModel.AvailableCategories = categories.Select(c => new CategoryFilterItem
        {
            Id = c.Id,
            Name = c.Name,
            Color = c.Color
        }).ToList();

        //viewModel.AvailableCategoriesSelectList = categories.Select(c => new SelectListItem
        //{
        //    Value = c.Id.ToString(),
        //    Text = c.Name,
        //    Selected = c.Id == viewModel.FilterByCategoryId
        //}).ToList();

        // Tworzymy SelectList z opcją "Uncategorized"
        var categorySelectList = categories.Select(c => new SelectListItem
        {
            Value = c.Id.ToString(),
            Text = c.Name,
            Selected = c.Id == viewModel.FilterByCategoryId
        }).ToList();

        // Dodaj opcję "Uncategorized" (ID = 0)
        categorySelectList.Insert(0, new SelectListItem
        {
            Value = "0",
            Text = "Uncategorized",
            Selected = viewModel.FilterByCategoryId == 0
        });

        viewModel.AvailableCategoriesSelectList = categorySelectList;
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("User is not authenticated");
    }
}
