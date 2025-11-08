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
    private readonly IGenericService<Category> _categoryService;
    private readonly ITripParticipantService _tripParticipantService;

    public BudgetSummaryController(
        IExpenseService expenseService,
        ITripService tripService,
        IGenericService<Category> categoryService,
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
        var categories = await _categoryService.GetAllAsync();

        // Dodaj kategorie do ViewModel
        viewModel.AvailableCategories = categories.Select(c => new CategoryFilterItem
        {
            Id = c.Id,
            Name = c.Name,
            Color = c.Color
        }).ToList();

        viewModel.AvailableCategoriesSelectList = categories.Select(c => new SelectListItem
        {
            Value = c.Id.ToString(),
            Text = c.Name,
            Selected = c.Id == viewModel.FilterByCategoryId
        }).ToList();
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("User is not authenticated");
    }
}
