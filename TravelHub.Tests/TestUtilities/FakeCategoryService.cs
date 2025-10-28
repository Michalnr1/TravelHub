using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Services;

namespace TravelHub.Tests.TestUtilities;

public class FakeCategoryService : ICategoryService
{
    private readonly List<Category> _categories = new();
    private readonly List<Activity> _activities = new();
    private readonly List<Expense> _expenses = new();
    private int _nextCatId = 1;
    private int _nextActId = 1;
    private int _nextExpId = 1;

    // Tracking for assertions
    public bool AddCalled { get; private set; }
    public bool UpdateCalled { get; private set; }
    public bool DeleteCalled { get; private set; }

    public Task<Category?> GetByIdWithRelatedDataAsync(int id)
    {
        var cat = _categories.FirstOrDefault(c => c.Id == id);
        if (cat == null) return Task.FromResult<Category?>(null);

        var clone = new Category
        {
            Id = cat.Id,
            Name = cat.Name,
            Color = cat.Color,
            Activities = _activities.Where(a => a.CategoryId == id).ToList(),
            Expenses = _expenses.Where(e => e.CategoryId == id).ToList()
        };

        return Task.FromResult<Category?>(clone);
    }

    public Task<bool> ExistsByNameAsync(string name)
    {
        var exists = _categories.Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(exists);
    }

    public Task<bool> IsInUseAsync(int categoryId)
    {
        var used = _activities.Any(a => a.CategoryId == categoryId) || _expenses.Any(e => e.CategoryId == categoryId);
        return Task.FromResult(used);
    }

    public Task<Category> GetByIdAsync(object id)
    {
        var intId = Convert.ToInt32(id);
        var cat = _categories.FirstOrDefault(c => c.Id == intId);
        if (cat == null) throw new InvalidOperationException($"Category {intId} not found.");
        return Task.FromResult(cat);
    }

    public Task<IReadOnlyList<Category>> GetAllAsync()
    {
        return Task.FromResult((IReadOnlyList<Category>)_categories.Select(c => new Category
        {
            Id = c.Id,
            Name = c.Name,
            Color = c.Color
        }).ToList());
    }

    public Task<Category> AddAsync(Category entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (entity.Id == 0) entity.Id = _nextCatId++;
        _categories.Add(new Category { Id = entity.Id, Name = entity.Name, Color = entity.Color });
        AddCalled = true;
        return Task.FromResult(entity);
    }

    public Task UpdateAsync(Category entity)
    {
        var existing = _categories.FirstOrDefault(c => c.Id == entity.Id);
        if (existing == null) throw new InvalidOperationException($"Category {entity.Id} not found.");
        existing.Name = entity.Name;
        existing.Color = entity.Color;
        UpdateCalled = true;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(object id)
    {
        var intId = Convert.ToInt32(id);
        var removed = _categories.RemoveAll(c => c.Id == intId);
        DeleteCalled = removed > 0;
        return Task.CompletedTask;
    }

    // Helpers for tests
    public Category SeedCategory(string name, string color = "#000000")
    {
        var cat = new Category { Id = _nextCatId++, Name = name, Color = color };
        _categories.Add(cat);
        return cat;
    }

    public void AddActivity(Activity activity)
    {
        if (activity.Id == 0) activity.Id = _nextActId++;
        _activities.Add(activity);
    }

    public void AddExpense(Expense expense)
    {
        if (expense.Id == 0) expense.Id = _nextExpId++;
        _expenses.Add(expense);
    }
}

// Minimal TempData provider for tests
public class TestTempDataProvider : ITempDataProvider
{
    public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
    public void SaveTempData(HttpContext context, IDictionary<string, object> values) { /* no-op */ }
}

// Minimal logger to avoid external deps
public class FakeLogger<T> : ILogger<T>
{
    public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
    public bool IsEnabled(LogLevel logLevel) => false;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }

    private class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new NullScope();
        public void Dispose() { }
    }
}