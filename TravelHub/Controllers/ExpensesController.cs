using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Web.ViewModels.Expenses;
using Microsoft.AspNetCore.Authorization;

namespace TravelHub.Web.Controllers;

[Authorize]
public class ExpensesController : Controller
{
    private readonly IExpenseService _expenseService;
    private readonly IGenericService<Currency> _currencyService;
    private readonly IGenericService<Category> _categoryService;
    private readonly UserManager<Person> _userManager;

    public ExpensesController(
        IExpenseService expenseService,
        IGenericService<Currency> currencyService,
        IGenericService<Category> categoryService,
        UserManager<Person> userManager)
    {
        _expenseService = expenseService;
        _currencyService = currencyService;
        _categoryService = categoryService;
        _userManager = userManager;
    }

    // GET: Expenses
    public async Task<IActionResult> Index()
    {
        var expenses = await _expenseService.GetAllAsync();
        var viewModel = expenses.Select(e => new ExpenseViewModel
        {
            Id = e.Id,
            Name = e.Name,
            Value = e.Value,
            PaidByName = e.PaidBy?.FirstName + " " + e.PaidBy?.LastName,
            CategoryName = e.Category?.Name,
            CurrencyName = e.Currency?.Name
        }).ToList();

        return View(viewModel);
    }

    // GET: Expenses/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var expense = await _expenseService.GetByIdAsync(id.Value);
        if (expense == null)
        {
            return NotFound();
        }

        var viewModel = new ExpenseDetailsViewModel
        {
            Id = expense.Id,
            Name = expense.Name,
            Value = expense.Value,
            PaidByName = expense.PaidBy?.FirstName + " " + expense.PaidBy?.LastName,
            CategoryName = expense.Category?.Name,
            CurrencyName = expense.Currency?.Name,
            CurrencyKey = expense.CurrencyKey,
            ParticipantNames = expense.Participants?.Select(p => p.FirstName + " " + p.LastName).ToList() ?? new List<string>()
        };

        return View(viewModel);
    }

    // GET: Expenses/Create
    public async Task<IActionResult> Create()
    {
        var viewModel = await CreateExpenseCreateEditViewModel();
        return View(viewModel);
    }

    // POST: Expenses/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ExpenseCreateEditViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            var expense = new Expense
            {
                Name = viewModel.Name,
                Value = viewModel.Value,
                PaidById = viewModel.PaidById,
                CategoryId = viewModel.CategoryId,
                CurrencyKey = viewModel.CurrencyKey
            };

            if (viewModel.SelectedParticipants != null && viewModel.SelectedParticipants.Any())
            {
                var participants = await _userManager.Users
                    .Where(p => viewModel.SelectedParticipants.Contains(p.Id))
                    .ToListAsync();
                expense.Participants = participants;
            }

            await _expenseService.AddAsync(expense);
            return RedirectToAction(nameof(Index));
        }

        await PopulateSelectLists(viewModel);
        return View(viewModel);
    }

    // GET: Expenses/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var expense = await _expenseService.GetByIdAsync(id.Value);
        if (expense == null)
        {
            return NotFound();
        }

        var viewModel = await CreateExpenseCreateEditViewModel(expense);
        return View(viewModel);
    }

    // POST: Expenses/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ExpenseCreateEditViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var existingExpense = await _expenseService.GetByIdAsync(id);
                if (existingExpense == null)
                {
                    return NotFound();
                }

                // Update basic properties
                existingExpense.Name = viewModel.Name;
                existingExpense.Value = viewModel.Value;
                existingExpense.PaidById = viewModel.PaidById;
                existingExpense.CategoryId = viewModel.CategoryId;
                existingExpense.CurrencyKey = viewModel.CurrencyKey;

                // Update participants
                if (viewModel.SelectedParticipants != null)
                {
                    var participants = await _userManager.Users
                        .Where(p => viewModel.SelectedParticipants.Contains(p.Id))
                        .ToListAsync();
                    existingExpense.Participants = participants;
                }
                else
                {
                    existingExpense.Participants.Clear();
                }

                await _expenseService.UpdateAsync(existingExpense);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ExpenseExists(viewModel.Id))
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

    // GET: Expenses/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var expense = await _expenseService.GetByIdAsync(id.Value);
        if (expense == null)
        {
            return NotFound();
        }

        var viewModel = new ExpenseDetailsViewModel
        {
            Id = expense.Id,
            Name = expense.Name,
            Value = expense.Value,
            PaidByName = expense.PaidBy?.FirstName + " " + expense.PaidBy?.LastName,
            CategoryName = expense.Category?.Name,
            CurrencyName = expense.Currency?.Name
        };

        return View(viewModel);
    }

    // POST: Expenses/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _expenseService.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> ExpenseExists(int id)
    {
        var expense = await _expenseService.GetByIdAsync(id);
        return expense != null;
    }

    private async Task<ExpenseCreateEditViewModel> CreateExpenseCreateEditViewModel(Expense expense = null)
    {
        var viewModel = new ExpenseCreateEditViewModel();

        if (expense != null)
        {
            viewModel.Id = expense.Id;
            viewModel.Name = expense.Name;
            viewModel.Value = expense.Value;
            viewModel.PaidById = expense.PaidById;
            viewModel.CategoryId = expense.CategoryId;
            viewModel.CurrencyKey = expense.CurrencyKey;
            viewModel.SelectedParticipants = expense.Participants?.Select(p => p.Id).ToList() ?? new List<string>();
        }

        await PopulateSelectLists(viewModel);
        return viewModel;
    }

    private async Task PopulateSelectLists(ExpenseCreateEditViewModel viewModel)
    {
        // Currencies
        var currencies = await _currencyService.GetAllAsync();
        viewModel.Currencies = currencies.Select(c => new CurrencySelectItem
        {
            Key = c.Key,
            Name = c.Name
        }).ToList();

        // Categories
        var categories = await _categoryService.GetAllAsync();
        viewModel.Categories = categories.Select(c => new CategorySelectItem
        {
            Id = c.Id,
            Name = c.Name
        }).ToList();

        // People - using UserManager to get all users
        var people = await _userManager.Users.ToListAsync();
        viewModel.People = people.Select(p => new PersonSelectItem
        {
            Id = p.Id,
            FullName = $"{p.FirstName} {p.LastName}",
            Email = p.Email
        }).ToList();

        viewModel.AllPeople = people.Select(p => new PersonSelectItem
        {
            Id = p.Id,
            FullName = $"{p.FirstName} {p.LastName}",
            Email = p.Email
        }).ToList();
    }
}