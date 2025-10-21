using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Web.ViewModels.Categories;

namespace TravelHub.Web.Controllers;

[Authorize]
public class CategoriesController : Controller
{
    private readonly ICategoryService _categoryService;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(ICategoryService categoryService, ILogger<CategoriesController> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }

    // GET: Category
    public async Task<IActionResult> Index()
    {
        var categories = await _categoryService.GetAllAsync();
        var vm = categories.Select(c => new CategoryViewModel
        {
            Id = c.Id,
            Name = c.Name,
            Color = c.Color
        }).ToList();

        return View(vm);
    }

    // GET: Category/Details/5
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var c = await _categoryService.GetByIdAsync(id);
            var vm = new CategoryViewModel { Id = c.Id, Name = c.Name, Color = c.Color };
            return View(vm);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    // GET: Category/Create
    public IActionResult Create()
    {
        return View(new CategoryViewModel { Color = "#000000" });
    }

    // POST: Category/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        if (await _categoryService.ExistsByNameAsync(model.Name))
        {
            ModelState.AddModelError(nameof(model.Name), "Category with that name already exists.");
            return View(model);
        }

        var category = new Category { Name = model.Name.Trim(), Color = model.Color };
        await _categoryService.AddAsync(category);

        TempData["SuccessMessage"] = "Category created.";
        return RedirectToAction(nameof(Index));
    }

    // GET: Category/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var c = await _categoryService.GetByIdAsync(id);
            var vm = new CategoryViewModel { Id = c.Id, Name = c.Name, Color = c.Color };
            return View(vm);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    // POST: Category/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CategoryViewModel model)
    {
        if (id != model.Id) return NotFound();
        if (!ModelState.IsValid) return View(model);

        try
        {
            // check name conflict: allow same name for current id
            var all = await _categoryService.GetAllAsync();
            if (all.Any(e => e.Name.Equals(model.Name, StringComparison.OrdinalIgnoreCase) && e.Id != model.Id))
            {
                ModelState.AddModelError(nameof(model.Name), "Category with that name already exists.");
                return View(model);
            }

            var category = await _categoryService.GetByIdAsync(model.Id);
            category.Name = model.Name.Trim();
            category.Color = model.Color;

            await _categoryService.UpdateAsync(category);

            TempData["SuccessMessage"] = "Category updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category");
            ModelState.AddModelError("", "Error updating category.");
            return View(model);
        }
    }

    // GET: Category/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var c = await _categoryService.GetByIdAsync(id);
            var vm = new CategoryViewModel { Id = c.Id, Name = c.Name, Color = c.Color };
            return View(vm);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    // POST: Category/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            if (await _categoryService.IsInUseAsync(id))
            {
                // Nie usuwamy — pokazujemy komunikat i wracamy do strony potwierdzenia delete
                TempData["ErrorMessage"] = "Category cannot be deleted — it is used in activities or expenses.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            await _categoryService.DeleteAsync(id);
            TempData["SuccessMessage"] = "Category deleted.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category");
            TempData["ErrorMessage"] = "Error deleting category.";
            return RedirectToAction(nameof(Delete), new { id });
        }
    }
}