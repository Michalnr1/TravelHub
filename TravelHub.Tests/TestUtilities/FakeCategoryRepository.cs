using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;

namespace TravelHub.Tests.TestUtilities
{
    public class FakeCategoryRepository : ICategoryRepository
    {
        private readonly List<Category> _categories = new();
        private readonly List<Activity> _activities = new();
        private readonly List<Expense> _expenses = new();
        private int _nextCatId = 1;
        private int _nextActId = 1;
        private int _nextExpId = 1;

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

        public Task<Category?> GetByIdAsync(object id)
        {
            var intId = Convert.ToInt32(id);
            var cat = _categories.FirstOrDefault(c => c.Id == intId);
            return Task.FromResult(cat);
        }

        public Task<IReadOnlyList<Category>> GetAllAsync()
        {
            return Task.FromResult((IReadOnlyList<Category>)_categories.ToList());
        }

        public Task<Category> AddAsync(Category entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (entity.Id == 0) entity.Id = _nextCatId++;
            _categories.Add(entity);
            return Task.FromResult(entity);
        }

        public Task UpdateAsync(Category entity)
        {
            var existing = _categories.FirstOrDefault(c => c.Id == entity.Id);
            if (existing != null)
            {
                existing.Name = entity.Name;
                existing.Color = entity.Color;
            }
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Category entity)
        {
            _categories.RemoveAll(c => c.Id == entity.Id);
            return Task.CompletedTask;
        }

        // Helpery do seedowania aktywności/wydatków w testach
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
}