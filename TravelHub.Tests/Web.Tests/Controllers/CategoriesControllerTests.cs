using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Web.Controllers;
using TravelHub.Web.ViewModels.Categories;
using Xunit;
using Microsoft.AspNetCore.Identity;

namespace TravelHub.Tests.Web.Tests.Controllers;

public class CategoriesControllerTests
{
    private readonly Mock<ICategoryService> _categoryServiceMock;
    private readonly Mock<UserManager<Person>> _userManagerMock;
    private readonly Mock<ILogger<CategoriesController>> _loggerMock;

    public CategoriesControllerTests()
    {
        _categoryServiceMock = new Mock<ICategoryService>();
        _userManagerMock = CreateMockUserManager();
        _loggerMock = new Mock<ILogger<CategoriesController>>();
    }

    private CategoriesController CreateController(
        Mock<ICategoryService> categoryServiceMock = null,
        Mock<UserManager<Person>> userManagerMock = null,
        string userId = "testUserId",
        Mock<ILogger<CategoriesController>> loggerMock = null)
    {
        var service = categoryServiceMock?.Object ?? _categoryServiceMock.Object;
        var userManager = userManagerMock?.Object ?? _userManagerMock.Object;
        var logger = loggerMock?.Object ?? _loggerMock.Object;

        var controller = new CategoriesController(service, logger, userManager);

        // Ustawiamy kontekst użytkownika
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, "test@example.com")
        }, "mock"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        // Provide TempData
        controller.TempData = new TempDataDictionary(new DefaultHttpContext(), new Mock<ITempDataProvider>().Object);

        return controller;
    }

    private Mock<UserManager<Person>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<Person>>();
        return new Mock<UserManager<Person>>(
            store.Object,
            null, null, null, null, null, null, null, null);
    }

    [Fact]
    public async Task Index_ReturnsViewWithList()
    {
        // Arrange
        var categories = new List<Category>
        {
            new Category { Id = 1, Name = "A", Color = "#111111", PersonId = "test" },
            new Category { Id = 2, Name = "B", Color = "#222222", PersonId = "test" }
        };

        _categoryServiceMock
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync((IReadOnlyList<Category>)categories);

        _categoryServiceMock.Setup(s => s.GetAllCategoriesByUserAsync(It.IsAny<string>()))
            .ReturnsAsync(categories);

        var controller = CreateController();

        // Act
        var result = await controller.Index();

        // Assert
        var vr = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<CategoryViewModel>>(vr.Model);
        Assert.Equal(2, model.Count);
        Assert.Contains(model, m => m.Name == "A");
        Assert.Contains(model, m => m.Name == "B");
    }

    [Fact]
    public async Task Details_ReturnsView_WhenFound()
    {
        // Arrange
        var cat = new Category { Id = 5, Name = "Detail", Color = "#abc123", PersonId = "test" };

        _categoryServiceMock
            .Setup(s => s.GetByIdAsync(cat.Id))
            .ReturnsAsync(cat);

        var controller = CreateController();

        // Act
        var result = await controller.Details(cat.Id);

        // Assert
        var vr = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<CategoryViewModel>(vr.Model);
        Assert.Equal(cat.Id, vm.Id);
        Assert.Equal(cat.Name, vm.Name);
    }

    [Fact]
    public async Task Details_ReturnsNotFound_WhenNotFound()
    {
        // Arrange
        _categoryServiceMock
            .Setup(s => s.GetByIdAsync(It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException());

        var controller = CreateController();

        // Act
        var result = await controller.Details(999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void Create_Get_ReturnsDefaultColor()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = controller.Create();

        // Assert
        var vr = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<CategoryViewModel>(vr.Model);
        Assert.Equal("#000000", vm.Color);
    }

    [Fact]
    public async Task Create_Post_RedirectsToIndex_WhenValidAndUnique()
    {
        // Arrange
        var mockCategoryService = new Mock<ICategoryService>();
        mockCategoryService
            .Setup(s => s.ExistsByNameAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        mockCategoryService
            .Setup(s => s.AddAsync(It.IsAny<Category>()))
            .ReturnsAsync((Category c) => c);

        var mockUserManager = CreateMockUserManager();
        var userId = "testUserId";

        mockUserManager
            .Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(userId);

        var controller = CreateController(
            categoryServiceMock: mockCategoryService,
            userManagerMock: mockUserManager,
            userId: userId);

        var model = new CategoryViewModel { Name = "New", Color = "#ffffff" };

        // Act
        var result = await controller.Create(model);

        // Assert
        var rr = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(controller.Index), rr.ActionName);

        mockCategoryService.Verify(s => s.AddAsync(It.Is<Category>(c =>
            c.Name == "New" &&
            c.Color == "#ffffff" &&
            c.PersonId == userId
        )), Times.Once);
    }

    [Fact]
    public async Task Edit_Get_ReturnsView_WhenFound()
    {
        // Arrange
        var cat = new Category { Id = 7, Name = "ToEdit", Color = "#010101", PersonId = "test" };

        _categoryServiceMock
            .Setup(s => s.GetByIdAsync(cat.Id))
            .ReturnsAsync(cat);

        var controller = CreateController();

        // Act
        var result = await controller.Edit(cat.Id);

        // Assert
        var vr = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<CategoryViewModel>(vr.Model);
        Assert.Equal(cat.Name, vm.Name);
    }

    [Fact]
    public async Task Edit_Post_ReturnsNotFound_WhenIdMismatch()
    {
        // Arrange
        var controller = CreateController();

        var model = new CategoryViewModel { Id = 1, Name = "X", Color = "#000000" };

        // Act
        var result = await controller.Edit(2, model);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Post_ReturnsView_WhenNameConflict()
    {
        // Arrange
        var c1 = new Category { Id = 1, Name = "A", Color = "#111111", PersonId = "test" };
        var c2 = new Category { Id = 2, Name = "B", Color = "#222222", PersonId = "test" };

        var mockCategoryService = new Mock<ICategoryService>();
        mockCategoryService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync((IReadOnlyList<Category>)new List<Category> { c1, c2 });

        var controller = CreateController(categoryServiceMock: mockCategoryService);

        var model = new CategoryViewModel { Id = c2.Id, Name = "A", Color = "#222222" };

        // Act
        var result = await controller.Edit(c2.Id, model);

        // Assert
        var vr = Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey(nameof(model.Name)));

        mockCategoryService.Verify(s => s.UpdateAsync(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task Edit_Post_RedirectsToIndex_OnSuccess()
    {
        // Arrange
        var existing = new Category { Id = 3, Name = "Original", Color = "#111111", PersonId = "test" };

        var mockCategoryService = new Mock<ICategoryService>();
        mockCategoryService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync((IReadOnlyList<Category>)new List<Category> { existing });

        mockCategoryService
            .Setup(s => s.GetByIdAsync(existing.Id))
            .ReturnsAsync(existing);

        mockCategoryService
            .Setup(s => s.UpdateAsync(It.IsAny<Category>()))
            .Returns(Task.CompletedTask);

        var controller = CreateController(categoryServiceMock: mockCategoryService);

        var model = new CategoryViewModel { Id = existing.Id, Name = "Updated", Color = "#222222" };

        // Act
        var result = await controller.Edit(existing.Id, model);

        // Assert
        var rr = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(controller.Index), rr.ActionName);

        mockCategoryService.Verify(s => s.UpdateAsync(It.Is<Category>(c =>
            c.Id == existing.Id &&
            c.Name == "Updated" &&
            c.Color == "#222222"
        )), Times.Once);
    }

    [Fact]
    public async Task Delete_Get_ReturnsView_WhenFound()
    {
        // Arrange
        var c = new Category { Id = 8, Name = "ToDelete", Color = "#333333", PersonId = "test" };

        _categoryServiceMock
            .Setup(s => s.GetByIdAsync(c.Id))
            .ReturnsAsync(c);

        var controller = CreateController();

        // Act
        var result = await controller.Delete(c.Id);

        // Assert
        var vr = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<CategoryViewModel>(vr.Model);
        Assert.Equal(c.Name, vm.Name);
    }

    [Fact]
    public async Task DeleteConfirmed_RedirectsToIndex_WhenDeleted()
    {
        // Arrange
        var c = new Category { Id = 11, Name = "Unused", Color = "#555555", PersonId = "test" };

        var mockCategoryService = new Mock<ICategoryService>();
        mockCategoryService
            .Setup(s => s.IsInUseAsync(c.Id))
            .ReturnsAsync(false);

        mockCategoryService
            .Setup(s => s.DeleteAsync(c.Id))
            .Returns(Task.CompletedTask);

        var controller = CreateController(categoryServiceMock: mockCategoryService);

        // Act
        var result = await controller.DeleteConfirmed(c.Id);

        // Assert
        var rr = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(controller.Index), rr.ActionName);

        mockCategoryService.Verify(s => s.DeleteAsync(c.Id), Times.Once);
    }
}