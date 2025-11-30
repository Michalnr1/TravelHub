using System;
using System.Threading.Tasks;
using Moq;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Infrastructure.Services;
using Xunit;

namespace TravelHub.Tests.Infrastructure.Tests.Services;

public class CategoryServiceTests
{
    private readonly Mock<ICategoryRepository> _mockCategoryRepo;
    private readonly CategoryService _sut;

    public CategoryServiceTests()
    {
        _mockCategoryRepo = new Mock<ICategoryRepository>();
        _sut = new CategoryService(_mockCategoryRepo.Object);
    }

    [Fact]
    public async Task ExistsByNameAsync_ReturnsTrue_WhenRepositoryReturnsTrue()
    {
        // ARRANGE
        var name = "Hiking";
        _mockCategoryRepo
            .Setup(r => r.ExistsByNameAsync(name))
            .ReturnsAsync(true);

        // ACT
        var result = await _sut.ExistsByNameAsync(name);

        // ASSERT
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsByNameAsync_ReturnsFalse_WhenRepositoryReturnsFalse()
    {
        // ARRANGE
        var name = "Swimming";
        _mockCategoryRepo
            .Setup(r => r.ExistsByNameAsync(name))
            .ReturnsAsync(false);

        // ACT
        var result = await _sut.ExistsByNameAsync(name);

        // ASSERT
        Assert.False(result);
    }

    [Fact]
    public async Task IsInUseAsync_ReturnsTrue_WhenRepositoryReportsInUse()
    {
        // ARRANGE
        const int categoryId = 5;
        _mockCategoryRepo
            .Setup(r => r.IsInUseAsync(categoryId))
            .ReturnsAsync(true);

        // ACT
        var result = await _sut.IsInUseAsync(categoryId);

        // ASSERT
        Assert.True(result);
    }

    [Fact]
    public async Task IsInUseAsync_ReturnsFalse_WhenRepositoryReportsNotInUse()
    {
        // ARRANGE
        const int categoryId = 6;
        _mockCategoryRepo
            .Setup(r => r.IsInUseAsync(categoryId))
            .ReturnsAsync(false);

        // ACT
        var result = await _sut.IsInUseAsync(categoryId);

        // ASSERT
        Assert.False(result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsCategory_WhenFound()
    {
        // ARRANGE
        var category = new Category { Id = 10, Name = "Found", Color = "#fff", PersonId = "test" };
        _mockCategoryRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<object>()))
            .ReturnsAsync(category);

        // ACT
        var result = await _sut.GetByIdAsync(category.Id);

        // ASSERT
        Assert.Equal(category.Id, result.Id);
        Assert.Equal(category.Name, result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsInvalidOperation_WhenNotFound()
    {
        // ARRANGE
        _mockCategoryRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<object>()))
            .ReturnsAsync((Category?)null);

        // ACT & ASSERT
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.GetByIdAsync(999));
    }
}
