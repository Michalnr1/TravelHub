using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TravelHub.Infrastructure;
using TravelHub.Infrastructure.Repositories;
using TravelHub.Domain.Entities;
using Xunit;

namespace TravelHub.Tests.Repositories
{
    public class CategoryRepositoryTests
    {
        private static ApplicationDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task GetByIdWithRelatedDataAsync_ReturnsCategoryWithActivitiesAndExpenses()
        {
            var dbName = Guid.NewGuid().ToString();
            await using var context = CreateContext(dbName);

            var category = new Category { Name = "Outdoor", Color = "#00FF00" };
            context.Categories.Add(category);
            await context.SaveChangesAsync();

            var activity = new Activity { Name = "Hiking", CategoryId = category.Id };
            var expense = new Expense { Name = "Guide fee", Value = 100m, CategoryId = category.Id, PaidById = "test" };

            context.Activities.Add(activity);
            context.Expenses.Add(expense);
            await context.SaveChangesAsync();

            var repo = new CategoryRepository(context);
            var result = await repo.GetByIdWithRelatedDataAsync(category.Id);

            Assert.NotNull(result);
            Assert.Equal(category.Id, result!.Id);
            Assert.Contains(result.Activities, a => a.Id == activity.Id);
            Assert.Contains(result.Expenses, e => e.Id == expense.Id);
        }

        [Fact]
        public async Task ExistsByNameAsync_IsCaseInsensitive()
        {
            var dbName = Guid.NewGuid().ToString();
            await using var context = CreateContext(dbName);

            context.Categories.Add(new Category { Name = "Hiking", Color = "#111111" });
            await context.SaveChangesAsync();

            var repo = new CategoryRepository(context);

            Assert.True(await repo.ExistsByNameAsync("hIkInG"));
            Assert.True(await repo.ExistsByNameAsync("HIKING"));
            Assert.False(await repo.ExistsByNameAsync("Swimming"));
        }

        [Fact]
        public async Task IsInUseAsync_ReturnsTrue_WhenActivityReferencesCategory()
        {
            var dbName = Guid.NewGuid().ToString();
            await using var context = CreateContext(dbName);

            var category = new Category { Name = "Sport", Color = "#222222" };
            context.Categories.Add(category);
            await context.SaveChangesAsync();

            context.Activities.Add(new Activity { Name = "Football", CategoryId = category.Id });
            await context.SaveChangesAsync();

            var repo = new CategoryRepository(context);

            Assert.True(await repo.IsInUseAsync(category.Id));
        }

        [Fact]
        public async Task IsInUseAsync_ReturnsTrue_WhenExpenseReferencesCategory()
        {
            var dbName = Guid.NewGuid().ToString();
            await using var context = CreateContext(dbName);

            var category = new Category { Name = "Meals", Color = "#333333" };
            context.Categories.Add(category);
            await context.SaveChangesAsync();

            context.Expenses.Add(new Expense { Name = "Lunch", Value = 25m, CategoryId = category.Id, PaidById = "test" });
            await context.SaveChangesAsync();

            var repo = new CategoryRepository(context);

            Assert.True(await repo.IsInUseAsync(category.Id));
        }

        [Fact]
        public async Task IsInUseAsync_ReturnsFalse_WhenNoReferences()
        {
            var dbName = Guid.NewGuid().ToString();
            await using var context = CreateContext(dbName);

            var category = new Category { Name = "Unused", Color = "#444444" };
            context.Categories.Add(category);
            await context.SaveChangesAsync();

            var repo = new CategoryRepository(context);

            Assert.False(await repo.IsInUseAsync(category.Id));
        }
    }
}
