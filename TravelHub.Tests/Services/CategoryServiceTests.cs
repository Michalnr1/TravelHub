using System;
using System.Threading.Tasks;
using TravelHub.Infrastructure.Services;
using TravelHub.Domain.Entities;
using TravelHub.Tests.TestUtilities;
using Xunit;

namespace TravelHub.Tests.Services
{
    public class CategoryServiceTests
    {
        [Fact]
        public async Task ExistsByNameAsync_IsCaseInsensitive_DelegatesToRepository()
        {
            var repo = new FakeCategoryRepository();
            await repo.AddAsync(new Category { Name = "Hiking", Color = "#111111" });

            var service = new CategoryService(repo);

            Assert.True(await service.ExistsByNameAsync("hIkInG"));
            Assert.True(await service.ExistsByNameAsync("HIKING"));
            Assert.False(await service.ExistsByNameAsync("Swimming"));
        }

        [Fact]
        public async Task IsInUseAsync_ReturnsTrue_WhenActivityReferencesCategory()
        {
            var repo = new FakeCategoryRepository();
            var cat = await repo.AddAsync(new Category { Name = "Sport", Color = "#222222" });
            repo.AddActivity(new Activity { Name = "Football", CategoryId = cat.Id });

            var service = new CategoryService(repo);

            Assert.True(await service.IsInUseAsync(cat.Id));
        }

        [Fact]
        public async Task IsInUseAsync_ReturnsTrue_WhenExpenseReferencesCategory()
        {
            var repo = new FakeCategoryRepository();
            var cat = await repo.AddAsync(new Category { Name = "Meals", Color = "#333333" });
            repo.AddExpense(new Expense { Name = "Lunch", Value = 25m, CategoryId = cat.Id, PaidById = "test" });

            var service = new CategoryService(repo);

            Assert.True(await service.IsInUseAsync(cat.Id));
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsEntity_WhenFound()
        {
            var repo = new FakeCategoryRepository();
            var cat = await repo.AddAsync(new Category { Name = "Found", Color = "#123456" });

            var service = new CategoryService(repo);
            var result = await service.GetByIdAsync(cat.Id);

            Assert.Equal(cat.Id, result.Id);
            Assert.Equal(cat.Name, result.Name);
        }

        [Fact]
        public async Task GetByIdAsync_ThrowsInvalidOperation_WhenNotFound()
        {
            var repo = new FakeCategoryRepository();
            var service = new CategoryService(repo);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetByIdAsync(999));
        }
    }
}
