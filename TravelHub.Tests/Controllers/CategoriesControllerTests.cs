using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using TravelHub.Web.Controllers;
using TravelHub.Web.ViewModels.Categories;
using TravelHub.Tests.TestUtilities;
using Xunit;

namespace TravelHub.Tests.Controllers;

public class CategoriesControllerTests
{
    private static CategoriesController CreateController(FakeCategoryService service)
    {
        var controller = new CategoriesController(service, new FakeLogger<CategoriesController>());
        // Provide TempData so controller can safely set TempData["..."]
        controller.TempData = new TempDataDictionary(new DefaultHttpContext(), new TestTempDataProvider());
        return controller;
    }

    [Fact]
    public async Task Index_ReturnsViewWithList()
    {
        var svc = new FakeCategoryService();
        svc.SeedCategory("A", "#111111");
        svc.SeedCategory("B", "#222222");

        var controller = CreateController(svc);
        var result = await controller.Index();

        var vr = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<System.Collections.Generic.List<CategoryViewModel>>(vr.Model);
        Assert.Equal(2, model.Count);
        Assert.Contains(model, m => m.Name == "A");
        Assert.Contains(model, m => m.Name == "B");
    }

    [Fact]
    public async Task Details_ReturnsView_WhenFound()
    {
        var svc = new FakeCategoryService();
        var cat = svc.SeedCategory("Detail", "#abc123");

        var controller = CreateController(svc);
        var result = await controller.Details(cat.Id);

        var vr = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<CategoryViewModel>(vr.Model);
        Assert.Equal(cat.Id, vm.Id);
        Assert.Equal(cat.Name, vm.Name);
    }

    [Fact]
    public async Task Details_ReturnsNotFound_WhenNotFound()
    {
        var svc = new FakeCategoryService();
        var controller = CreateController(svc);

        var result = await controller.Details(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void Create_Get_ReturnsDefaultColor()
    {
        var svc = new FakeCategoryService();
        var controller = CreateController(svc);

        var result = controller.Create();
        var vr = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<CategoryViewModel>(vr.Model);
        Assert.Equal("#000000", vm.Color);
    }

    [Fact]
    public async Task Create_Post_RedirectsToIndex_WhenValidAndUnique()
    {
        var svc = new FakeCategoryService();
        var controller = CreateController(svc);

        var model = new CategoryViewModel { Name = "New", Color = "#ffffff" };
        var result = await controller.Create(model);

        var rr = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(controller.Index), rr.ActionName);
        Assert.True(svc.AddCalled);
    }

    [Fact]
    public async Task Create_Post_ReturnsView_WhenNameExists()
    {
        var svc = new FakeCategoryService();
        svc.SeedCategory("Exist", "#111111");
        var controller = CreateController(svc);

        var model = new CategoryViewModel { Name = "Exist", Color = "#ffffff" };
        var result = await controller.Create(model);

        var vr = Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey(nameof(model.Name)));
    }

    [Fact]
    public async Task Edit_Get_ReturnsView_WhenFound()
    {
        var svc = new FakeCategoryService();
        var cat = svc.SeedCategory("ToEdit", "#010101");
        var controller = CreateController(svc);

        var result = await controller.Edit(cat.Id);
        var vr = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<CategoryViewModel>(vr.Model);
        Assert.Equal(cat.Name, vm.Name);
    }

    [Fact]
    public async Task Edit_Post_ReturnsNotFound_WhenIdMismatch()
    {
        var svc = new FakeCategoryService();
        var controller = CreateController(svc);

        var model = new CategoryViewModel { Id = 1, Name = "X", Color = "#000000" };
        var result = await controller.Edit(2, model);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Post_ReturnsView_WhenNameConflict()
    {
        var svc = new FakeCategoryService();
        var c1 = svc.SeedCategory("A", "#111111");
        var c2 = svc.SeedCategory("B", "#222222");
        var controller = CreateController(svc);

        var model = new CategoryViewModel { Id = c2.Id, Name = "A", Color = "#222222" };
        var result = await controller.Edit(c2.Id, model);

        var vr = Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey(nameof(model.Name)));
    }

    [Fact]
    public async Task Edit_Post_RedirectsToIndex_OnSuccess()
    {
        var svc = new FakeCategoryService();
        var c = svc.SeedCategory("Original", "#111111");
        var controller = CreateController(svc);

        var model = new CategoryViewModel { Id = c.Id, Name = "Updated", Color = "#222222" };
        var result = await controller.Edit(c.Id, model);

        var rr = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(controller.Index), rr.ActionName);
        Assert.True(svc.UpdateCalled);
    }

    [Fact]
    public async Task Delete_Get_ReturnsView_WhenFound()
    {
        var svc = new FakeCategoryService();
        var c = svc.SeedCategory("ToDelete", "#333333");
        var controller = CreateController(svc);

        var result = await controller.Delete(c.Id);

        var vr = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<CategoryViewModel>(vr.Model);
        Assert.Equal(c.Name, vm.Name);
    }

    [Fact]
    public async Task DeleteConfirmed_RedirectsToDelete_WhenInUse()
    {
        var svc = new FakeCategoryService();
        var c = svc.SeedCategory("Used", "#444444");
        // mark used
        svc.AddActivity(new TravelHub.Domain.Entities.Activity { Name = "A", CategoryId = c.Id, TripId = 1 });
        var controller = CreateController(svc);

        var result = await controller.DeleteConfirmed(c.Id);
        var rr = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(controller.Delete), rr.ActionName);
        Assert.Equal(c.Id, rr.RouteValues?["id"]);
    }

    [Fact]
    public async Task DeleteConfirmed_RedirectsToIndex_WhenDeleted()
    {
        var svc = new FakeCategoryService();
        var c = svc.SeedCategory("Unused", "#555555");
        var controller = CreateController(svc);

        var result = await controller.DeleteConfirmed(c.Id);
        var rr = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(controller.Index), rr.ActionName);
        Assert.True(svc.DeleteCalled);
    }
}
