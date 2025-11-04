using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Web.Controllers;
using TravelHub.Web.ViewModels.Categories;
using TravelHub.Tests.TestUtilities;
using Xunit;

namespace TravelHub.Tests.Web.Tests.Controllers;

public class CategoriesControllerTests
{
    private static CategoriesController CreateController(ICategoryService service)
    {
        var controller = new CategoriesController(service, new FakeLogger<CategoriesController>());
        // Provide TempData so controller can safely set TempData["..."]
        controller.TempData = new TempDataDictionary(new DefaultHttpContext(), new TestTempDataProvider());
        return controller;
    }

    [Fact]
    public async Task Index_ReturnsViewWithList()
    {
        var mock = new Mock<ICategoryService>();
        var categories = new List<Category>
        {
            new Category { Id = 1, Name = "A", Color = "#111111" },
            new Category { Id = 2, Name = "B", Color = "#222222" }
        };
        mock.Setup(s => s.GetAllAsync()).ReturnsAsync((IReadOnlyList<Category>)categories);

        var controller = CreateController(mock.Object);
        var result = await controller.Index();

        var vr = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<CategoryViewModel>>(vr.Model);
        Assert.Equal(2, model.Count);
        Assert.Contains(model, m => m.Name == "A");
        Assert.Contains(model, m => m.Name == "B");
    }

    [Fact]
    public async Task Details_ReturnsView_WhenFound()
    {
        var mock = new Mock<ICategoryService>();
        var cat = new Category { Id = 5, Name = "Detail", Color = "#abc123" };
        mock.Setup(s => s.GetByIdAsync(cat.Id)).ReturnsAsync(cat);

        var controller = CreateController(mock.Object);
        var result = await controller.Details(cat.Id);

        var vr = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<CategoryViewModel>(vr.Model);
        Assert.Equal(cat.Id, vm.Id);
        Assert.Equal(cat.Name, vm.Name);
    }

    [Fact]
    public async Task Details_ReturnsNotFound_WhenNotFound()
    {
        var mock = new Mock<ICategoryService>();
        mock.Setup(s => s.GetByIdAsync(It.IsAny<object>()))
            .ThrowsAsync(new InvalidOperationException());

        var controller = CreateController(mock.Object);
        var result = await controller.Details(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void Create_Get_ReturnsDefaultColor()
    {
        var mock = new Mock<ICategoryService>();
        var controller = CreateController(mock.Object);

        var result = controller.Create();
        var vr = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<CategoryViewModel>(vr.Model);
        Assert.Equal("#000000", vm.Color);
    }

    [Fact]
    public async Task Create_Post_RedirectsToIndex_WhenValidAndUnique()
    {
        var mock = new Mock<ICategoryService>();
        mock.Setup(s => s.ExistsByNameAsync(It.IsAny<string>())).ReturnsAsync(false);
        mock.Setup(s => s.AddAsync(It.IsAny<Category>())).ReturnsAsync((Category c) => c);

        var controller = CreateController(mock.Object);

        var model = new CategoryViewModel { Name = "New", Color = "#ffffff" };
        var result = await controller.Create(model);

        var rr = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(controller.Index), rr.ActionName);
        mock.Verify(s => s.AddAsync(It.Is<Category>(c => c.Name == "New" && c.Color == "#ffffff")), Times.Once);
    }

    [Fact]
    public async Task Create_Post_ReturnsView_WhenNameExists()
    {
        var mock = new Mock<ICategoryService>();
        mock.Setup(s => s.ExistsByNameAsync(It.IsAny<string>())).ReturnsAsync(true);

        var controller = CreateController(mock.Object);

        var model = new CategoryViewModel { Name = "Exist", Color = "#ffffff" };
        var result = await controller.Create(model);

        var vr = Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey(nameof(model.Name)));
        mock.Verify(s => s.AddAsync(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task Edit_Get_ReturnsView_WhenFound()
    {
        var mock = new Mock<ICategoryService>();
        var cat = new Category { Id = 7, Name = "ToEdit", Color = "#010101" };
        mock.Setup(s => s.GetByIdAsync(cat.Id)).ReturnsAsync(cat);

        var controller = CreateController(mock.Object);

        var result = await controller.Edit(cat.Id);
        var vr = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<CategoryViewModel>(vr.Model);
        Assert.Equal(cat.Name, vm.Name);
    }

    [Fact]
    public async Task Edit_Post_ReturnsNotFound_WhenIdMismatch()
    {
        var mock = new Mock<ICategoryService>();
        var controller = CreateController(mock.Object);

        var model = new CategoryViewModel { Id = 1, Name = "X", Color = "#000000" };
        var result = await controller.Edit(2, model);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Post_ReturnsView_WhenNameConflict()
    {
        var mock = new Mock<ICategoryService>();
        var c1 = new Category { Id = 1, Name = "A", Color = "#111111" };
        var c2 = new Category { Id = 2, Name = "B", Color = "#222222" };

        mock.Setup(s => s.GetAllAsync()).ReturnsAsync((IReadOnlyList<Category>)new List<Category> { c1, c2 });

        var controller = CreateController(mock.Object);

        var model = new CategoryViewModel { Id = c2.Id, Name = "A", Color = "#222222" };
        var result = await controller.Edit(c2.Id, model);

        var vr = Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey(nameof(model.Name)));
        mock.Verify(s => s.UpdateAsync(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task Edit_Post_RedirectsToIndex_OnSuccess()
    {
        var mock = new Mock<ICategoryService>();
        var existing = new Category { Id = 3, Name = "Original", Color = "#111111" };

        // No conflicting name in GetAllAsync
        mock.Setup(s => s.GetAllAsync()).ReturnsAsync((IReadOnlyList<Category>)new List<Category> { existing });
        mock.Setup(s => s.GetByIdAsync(existing.Id)).ReturnsAsync(existing);
        mock.Setup(s => s.UpdateAsync(It.IsAny<Category>())).Returns(Task.CompletedTask);

        var controller = CreateController(mock.Object);

        var model = new CategoryViewModel { Id = existing.Id, Name = "Updated", Color = "#222222" };
        var result = await controller.Edit(existing.Id, model);

        var rr = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(controller.Index), rr.ActionName);
        mock.Verify(s => s.UpdateAsync(It.Is<Category>(c => c.Id == existing.Id && c.Name == "Updated" && c.Color == "#222222")), Times.Once);
    }

    [Fact]
    public async Task Delete_Get_ReturnsView_WhenFound()
    {
        var mock = new Mock<ICategoryService>();
        var c = new Category { Id = 8, Name = "ToDelete", Color = "#333333" };
        mock.Setup(s => s.GetByIdAsync(c.Id)).ReturnsAsync(c);

        var controller = CreateController(mock.Object);

        var result = await controller.Delete(c.Id);

        var vr = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<CategoryViewModel>(vr.Model);
        Assert.Equal(c.Name, vm.Name);
    }

    [Fact]
    public async Task DeleteConfirmed_RedirectsToDelete_WhenInUse()
    {
        var mock = new Mock<ICategoryService>();
        var c = new Category { Id = 9, Name = "Used", Color = "#444444" };
        mock.Setup(s => s.IsInUseAsync(c.Id)).ReturnsAsync(true);

        var controller = CreateController(mock.Object);

        var result = await controller.DeleteConfirmed(c.Id);
        var rr = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(controller.Delete), rr.ActionName);
        Assert.Equal(c.Id, rr.RouteValues?["id"]);
        mock.Verify(s => s.DeleteAsync(It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task DeleteConfirmed_RedirectsToIndex_WhenDeleted()
    {
        var mock = new Mock<ICategoryService>();
        var c = new Category { Id = 11, Name = "Unused", Color = "#555555" };
        mock.Setup(s => s.IsInUseAsync(c.Id)).ReturnsAsync(false);
        mock.Setup(s => s.DeleteAsync(c.Id)).Returns(Task.CompletedTask);

        var controller = CreateController(mock.Object);

        var result = await controller.DeleteConfirmed(c.Id);
        var rr = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(controller.Index), rr.ActionName);
        mock.Verify(s => s.DeleteAsync(c.Id), Times.Once);
    }
}
