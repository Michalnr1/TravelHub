using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using TravelHub.Domain.Entities;
using TravelHub.Infrastructure.Services;
using TravelHub.Tests.TestUtilities;
using TravelHub.Web.Controllers;
using TravelHub.Web.ViewModels.Activities;
using TravelHub.Domain.Interfaces.Services;
using Xunit;

namespace TravelHub.Tests.Controllers;

public class SpotsControllerTests
{
    private static SpotsController CreateController(
        SpotService spotService,
        ActivityService activityService,
        IGenericService<Category> categoryService,
        ITripService tripService,
        IGenericService<Day> dayService,
        IPhotoService photoService,
        string currentUserId = "test")
    {
        var userManager = TestUserManagerFactory.Create();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[] { new KeyValuePair<string, string?>("ApiKeys:GoogleApiKey", "test") })
            .Build();

        var controller = new SpotsController(
            spotService,
            activityService,
            categoryService,
            tripService,
            dayService,
            photoService,
            new FakeLogger<SpotsController>(),
            configuration,
            userManager);

        // Provide TempData
        controller.TempData = new TempDataDictionary(new DefaultHttpContext(), new TestTempDataProvider());

        // Setup HttpContext.User if user id provided
        var httpContext = new DefaultHttpContext();
        if (!string.IsNullOrEmpty(currentUserId))
        {
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, currentUserId) };
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        }
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        return controller;
    }

    [Fact]
    public async Task Index_ReturnsViewWithList()
    {
        var spotRepo = new FakeSpotRepository();
        var activityRepo = new FakeActivityRepository();
        var spotService = new SpotService(spotRepo, activityRepo);
        var activityService = new ActivityService(activityRepo);

        var trip = new Trip { Id = 1, Name = "Trip1", PersonId = "u", StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(1), Status = Status.Planning, IsPrivate = false };
        var cat = new Category { Id = 1, Name = "Cat", Color = "#000" };
        var day = new Day { Id = 2, Name = "Day1", TripId = trip.Id };

        var photo = new Photo { Id = 1, Name = "p1", SpotId = 0 }; // spotId ustawimy po seed
        var s = new Spot
        {
            Name = "S1",
            TripId = trip.Id,
            Trip = trip,
            CategoryId = cat.Id,
            Category = cat,
            DayId = day.Id,
            Day = day,
            Latitude = 10,
            Longitude = 20,
            Cost = 5m
        };
        var seeded = spotRepo.SeedSpot(s);
        photo.SpotId = seeded.Id;
        // dodajemy zdjęcie bezpośrednio do obiektu
        seeded.Photos.Add(photo);

        var controller = CreateController(spotService, activityService, new FakeCategoryService(), new FakeTripService(), new FakeDayService(), new FakePhotoService());
        var result = await controller.Index();

        var vr = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<System.Collections.Generic.List<SpotDetailsViewModel>>(vr.Model);
        Assert.Single(model);
        Assert.Equal("S1", model[0].Name);
        Assert.Equal(1, model[0].PhotoCount);
    }

    [Fact]
    public async Task Details_ReturnsView_WhenFoundAndOwner()
    {
        var spotRepo = new FakeSpotRepository();
        var activityRepo = new FakeActivityRepository();
        var spotService = new SpotService(spotRepo, activityRepo);
        var activityService = new ActivityService(activityRepo);

        var trip = new Trip { Id = 10, Name = "T", PersonId = "owner", StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(1), Status = Status.Planning, IsPrivate = false };
        var s = new Spot { Name = "DetailSpot", TripId = trip.Id, Trip = trip };
        var seeded = spotRepo.SeedSpot(s);

        var photoSvc = new FakePhotoService();
        photoSvc.SeedPhoto(new Photo { Id = 1, Name = "p", SpotId = seeded.Id });

        var tripSvc = new FakeTripService();
        tripSvc.SeedTrip(trip);

        var controller = CreateController(spotService, activityService, new FakeCategoryService(), tripSvc, new FakeDayService(), photoSvc, currentUserId: "owner");
        var result = await controller.Details(seeded.Id);

        var vr = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<SpotDetailsViewModel>(vr.Model);
        Assert.Equal(seeded.Id, vm.Id);
        Assert.Equal("DetailSpot", vm.Name);
        Assert.NotNull(vm.Photos);
        Assert.Single(vm.Photos);
    }

    [Fact]
    public async Task Details_ReturnsForbid_WhenUserNotOwner()
    {
        var spotRepo = new FakeSpotRepository();
        var activityRepo = new FakeActivityRepository();
        var spotService = new SpotService(spotRepo, activityRepo);
        var activityService = new ActivityService(activityRepo);

        var trip = new Trip { Id = 11, Name = "T", PersonId = "ownerX", StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(1), Status = Status.Planning, IsPrivate = false };
        var s = new Spot { Name = "DetailSpot2", TripId = trip.Id, Trip = trip };
        var seeded = spotRepo.SeedSpot(s);

        var controller = CreateController(spotService, activityService, new FakeCategoryService(), new FakeTripService(), new FakeDayService(), new FakePhotoService(), currentUserId: "notOwner");
        var result = await controller.Details(seeded.Id);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Details_ReturnsNotFound_WhenIdNull()
    {
        var spotRepo = new FakeSpotRepository();
        var activityRepo = new FakeActivityRepository();
        var spotService = new SpotService(spotRepo, activityRepo);
        var activityService = new ActivityService(activityRepo);

        var controller = CreateController(spotService, activityService, new FakeCategoryService(), new FakeTripService(), new FakeDayService(), new FakePhotoService());
        var result = await controller.Details(null);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_Get_ReturnsDefaultDurationAndOrder()
    {
        var spotRepo = new FakeSpotRepository();
        var activityRepo = new FakeActivityRepository();
        var spotService = new SpotService(spotRepo, activityRepo);
        var activityService = new ActivityService(activityRepo);

        var controller = CreateController(spotService, activityService, new FakeCategoryService(), new FakeTripService(), new FakeDayService(), new FakePhotoService());
        var result = await controller.Create();

        var vr = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<SpotCreateEditViewModel>(vr.Model);
        Assert.Equal("01:00", vm.DurationString);
        Assert.Equal(0, vm.Order);
    }

    [Fact]
    public async Task Create_Post_RedirectsToTripsDetails_WhenValid()
    {
        var spotRepo = new FakeSpotRepository();
        var activityRepo = new FakeActivityRepository();
        var spotService = new SpotService(spotRepo, activityRepo);
        var activityService = new ActivityService(activityRepo);

        var controller = CreateController(spotService, activityService, new FakeCategoryService(), new FakeTripService(), new FakeDayService(), new FakePhotoService());
        var model = new SpotCreateEditViewModel
        {
            Name = "NewSpot",
            TripId = 123,
            DurationString = "01:00",
            Longitude = 1.0,
            Latitude = 2.0,
            Cost = 0m
        };

        var result = await controller.Create(model);

        var rr = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", rr.ActionName);
        Assert.Equal("Trips", rr.ControllerName);
        Assert.Equal(123, rr.RouteValues?["id"]);
    }

    [Fact]
    public async Task Edit_Get_ReturnsForbid_WhenUserNotOwner()
    {
        var spotRepo = new FakeSpotRepository();
        var activityRepo = new FakeActivityRepository();
        var spotService = new SpotService(spotRepo, activityRepo);
        var activityService = new ActivityService(activityRepo);

        var trip = new Trip { Id = 20, Name = "T", PersonId = "ownerY", StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(1), Status = Status.Planning, IsPrivate = false };
        var s = new Spot { Name = "EditSpot", TripId = trip.Id, Trip = trip };
        var seeded = spotRepo.SeedSpot(s);

        var controller = CreateController(spotService, activityService, new FakeCategoryService(), new FakeTripService(), new FakeDayService(), new FakePhotoService(), currentUserId: "otherUser");
        var result = await controller.Edit(seeded.Id);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeleteConfirmed_RedirectsToTripsDetails_WhenDeleted()
    {
        var spotRepo = new FakeSpotRepository();
        var activityRepo = new FakeActivityRepository();
        var spotService = new SpotService(spotRepo, activityRepo);
        var activityService = new ActivityService(activityRepo);

        var trip = new Trip { Id = 30, Name = "TDel", PersonId = "u", StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(1), Status = Status.Planning, IsPrivate = false };
        var s = new Spot { Name = "ToDelete", TripId = trip.Id, Trip = trip, DayId = 5 };
        var seeded = spotRepo.SeedSpot(s);

        var controller = CreateController(spotService, activityService, new FakeCategoryService(), new FakeTripService(), new FakeDayService(), new FakePhotoService());
        var result = await controller.DeleteConfirmed(seeded.Id);

        var rr = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", rr.ActionName);
        Assert.Equal("Trips", rr.ControllerName);
        Assert.Equal(trip.Id, rr.RouteValues?["id"]);
    }
}
