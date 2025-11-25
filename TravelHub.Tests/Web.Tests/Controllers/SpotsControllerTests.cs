using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Infrastructure.Services;
using TravelHub.Tests.TestUtilities;
using TravelHub.Web.Controllers;
using TravelHub.Web.ViewModels.Activities;
using Xunit;

namespace TravelHub.Tests.Web.Tests.Controllers;

public class SpotsControllerTests
{
    private static SpotsController CreateController(
    Mock<ISpotService> spotMock,
    Mock<IActivityService> activityMock,
    Mock<IGenericService<Category>> categoryMock,
    Mock<ITripService> tripMock,
    Mock<ITripParticipantService> tripParticipantMock,
    Mock<IGenericService<Day>> dayMock,
    Mock<IPhotoService> photoMock,
    Mock<IReverseGeocodingService> reverseGeoMock,
    Mock<IExpenseService> expenseMock,
    Mock<IExchangeRateService> exchangeRateMock,
    string currentUserId = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[] { new KeyValuePair<string, string?>("ApiKeys:GoogleApiKey", "test") })
            .Build();

        // user manager (factory from your tests)
        var userManager = TestUserManagerFactory.Create();

        // create missing mocks required by constructor
        var fileMock = new Mock<IFileService>();
        var pdfMock = new Mock<IPdfService>();
        var viewEngineMock = new Mock<ICompositeViewEngine>();
        var tempDataProviderMock = new Mock<ITempDataProvider>();
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        var webHostEnvironmentMock = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
        var transportMock = new Mock<ITransportService>();

        // create the controller with all required parameters in correct order
        var controller = new SpotsController(
            spotMock.Object,
            activityMock.Object,
            categoryMock.Object,
            tripMock.Object,
            tripParticipantMock.Object,
            dayMock.Object,
            photoMock.Object,
            transportMock.Object,
            fileMock.Object,                // IFileService
            reverseGeoMock.Object,
            expenseMock.Object,
            exchangeRateMock.Object,
            new FakeLogger<SpotsController>(),
            configuration,
            userManager,                   // UserManager<Person>
            pdfMock.Object,                // IPdfService
            webHostEnvironmentMock.Object, // IWebHostEnvironment
            viewEngineMock.Object,         // ICompositeViewEngine
            tempDataProviderMock.Object,   // ITempDataProvider
            httpContextAccessorMock.Object // IHttpContextAccessor
        );

        // Prepare HttpContext and TempData
        var httpContext = new DefaultHttpContext();
        if (!string.IsNullOrEmpty(currentUserId))
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, currentUserId) }, "TestAuth"));
        }

        // make IHttpContextAccessor return the created context
        httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

        // If you want TempData to behave like real one in tests, you can use TestTempDataProvider (or the mock object)
        // Here we prefer to use a concrete TestTempDataProvider instance for TempDataDictionary
        var concreteTempDataProvider = new TestTempDataProvider();
        controller.TempData = new TempDataDictionary(httpContext, concreteTempDataProvider);

        // set ControllerContext with the httpContext (so ActionContext, Request, etc. are available)
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        controller.Url = new FakeUrlHelper();

        return controller;
    }


    [Fact]
    public async Task Index_ReturnsViewWithList()
    {
        // Arrange
        var spotMock = new Mock<ISpotService>();
        var activityMock = new Mock<IActivityService>();
        var categoryMock = new Mock<IGenericService<Category>>();
        var tripMock = new Mock<ITripService>();
        var tripParticipantMock = new Mock<ITripParticipantService>();
        var dayMock = new Mock<IGenericService<Day>>();
        var photoMock = new Mock<IPhotoService>();
        var reverseGeoMock = new Mock<IReverseGeocodingService>();
        var expenseMock = new Mock<IExpenseService>();
        var exchangeRateMock = new Mock<IExchangeRateService>();

        var spot = new Spot
        {
            Id = 1,
            Name = "S1",
            TripId = 1,
            Trip = new Trip { Id = 1, Name = "Trip1", PersonId = "test" },
            Category = new Category { Id = 1, Name = "Cat", Color = "0000000" },
            Day = new Day { Id = 2, Name = "Day1" },
            Photos = new List<Photo> { new Photo { Id = 1, Name = "p1", SpotId = 1, FilePath = "~/images/spots" } }
        };

        spotMock.Setup(s => s.GetAllWithDetailsAsync()).ReturnsAsync(new List<Spot> { spot });

        var controller = CreateController(spotMock, activityMock, categoryMock, tripMock, tripParticipantMock, dayMock, photoMock, reverseGeoMock, expenseMock, exchangeRateMock);

        // Act
        var result = await controller.Index();

        // Assert
        var vr = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<SpotDetailsViewModel>>(vr.Model);
        Assert.Single(model);
        Assert.Equal("S1", model[0].Name);
        Assert.Equal(1, model[0].PhotoCount);
    }

    [Fact]
    public async Task Details_ReturnsView_WhenFoundAndOwner()
    {
        // Arrange
        var spotMock = new Mock<ISpotService>();
        var activityMock = new Mock<IActivityService>();
        var categoryMock = new Mock<IGenericService<Category>>();
        var tripMock = new Mock<ITripService>();
        var tripParticipantMock = new Mock<ITripParticipantService>();
        var dayMock = new Mock<IGenericService<Day>>();
        var photoMock = new Mock<IPhotoService>();
        var reverseGeoMock = new Mock<IReverseGeocodingService>();
        var expenseMock = new Mock<IExpenseService>();
        var exchangeRateMock = new Mock<IExchangeRateService>();

        var trip = new Trip { Id = 10, PersonId = "owner", Name = "T" };
        var spot = new Spot { Id = 5, Name = "DetailSpot", TripId = trip.Id, Trip = trip, Photos = new List<Photo>() };

        spot.Photos.Add(new Photo { Id = 1, Name = "p", SpotId = spot.Id, FilePath = "~/images/spots" });

        spotMock.Setup(s => s.GetSpotDetailsAsync(spot.Id)).ReturnsAsync(spot);
        spotMock.Setup(s => s.UserOwnsSpotAsync(spot.Id, It.IsAny<string>())).ReturnsAsync(true);
        photoMock.Setup(p => p.GetBySpotIdAsync(spot.Id)).ReturnsAsync((IReadOnlyList<Photo>)spot.Photos);

        var controller = CreateController(spotMock, activityMock, categoryMock, tripMock, tripParticipantMock, dayMock, photoMock, reverseGeoMock, expenseMock, exchangeRateMock, currentUserId: "owner");

        // Act
        var result = await controller.Details(spot.Id);

        // Assert
        var vr = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<SpotDetailsViewModel>(vr.Model);
        Assert.Equal(spot.Id, vm.Id);
        Assert.Equal("DetailSpot", vm.Name);
        Assert.NotNull(vm.Photos);
        Assert.Single(vm.Photos);
    }

    [Fact]
    public async Task Details_ReturnsForbid_WhenUserNotOwner()
    {
        // Arrange
        var spotMock = new Mock<ISpotService>();
        var activityMock = new Mock<IActivityService>();
        var categoryMock = new Mock<IGenericService<Category>>();
        var tripMock = new Mock<ITripService>();
        var tripParticipantMock = new Mock<ITripParticipantService>();
        var dayMock = new Mock<IGenericService<Day>>();
        var photoMock = new Mock<IPhotoService>();
        var reverseGeoMock = new Mock<IReverseGeocodingService>();
        var expenseMock = new Mock<IExpenseService>();
        var exchangeRateMock = new Mock<IExchangeRateService>();

        var trip = new Trip { Id = 11, PersonId = "ownerX", Name = "T" };
        var spot = new Spot { Id = 6, Name = "DetailSpot2", TripId = trip.Id, Trip = trip };

        spotMock.Setup(s => s.GetSpotDetailsAsync(spot.Id)).ReturnsAsync(spot);
        spotMock.Setup(s => s.UserOwnsSpotAsync(spot.Id, It.IsAny<string>())).ReturnsAsync(false);

        var controller = CreateController(spotMock, activityMock, categoryMock, tripMock, tripParticipantMock, dayMock, photoMock, reverseGeoMock, expenseMock, exchangeRateMock, currentUserId: "notOwner");

        // Act
        var result = await controller.Details(spot.Id);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Details_ReturnsNotFound_WhenIdNull()
    {
        // Arrange
        var spotMock = new Mock<ISpotService>();
        var activityMock = new Mock<IActivityService>();
        var categoryMock = new Mock<IGenericService<Category>>();
        var tripMock = new Mock<ITripService>();
        var tripParticipantMock = new Mock<ITripParticipantService>();
        var dayMock = new Mock<IGenericService<Day>>();
        var photoMock = new Mock<IPhotoService>();
        var reverseGeoMock = new Mock<IReverseGeocodingService>();
        var expenseMock = new Mock<IExpenseService>();
        var exchangeRateMock = new Mock<IExchangeRateService>();

        var controller = CreateController(spotMock, activityMock, categoryMock, tripMock, tripParticipantMock, dayMock, photoMock, reverseGeoMock, expenseMock, exchangeRateMock);

        // Act
        var result = await controller.Details(null);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_Get_ReturnsDefaultDurationAndOrder()
    {
        // Arrange
        var spotMock = new Mock<ISpotService>();
        var activityMock = new Mock<IActivityService>();
        var categoryMock = new Mock<IGenericService<Category>>();
        var tripMock = new Mock<ITripService>();
        var tripParticipantMock = new Mock<ITripParticipantService>();
        var dayMock = new Mock<IGenericService<Day>>();
        var photoMock = new Mock<IPhotoService>();
        var reverseGeoMock = new Mock<IReverseGeocodingService>();
        var expenseMock = new Mock<IExpenseService>();
        var exchangeRateMock = new Mock<IExchangeRateService>();

        categoryMock.Setup(c => c.GetAllAsync()).ReturnsAsync((IReadOnlyList<Category>)Array.Empty<Category>());
        tripMock.Setup(t => t.GetAllAsync()).ReturnsAsync((IReadOnlyList<Trip>)Array.Empty<Trip>());
        dayMock.Setup(d => d.GetAllAsync()).ReturnsAsync((IReadOnlyList<Day>)Array.Empty<Day>());

        var controller = CreateController(spotMock, activityMock, categoryMock, tripMock, tripParticipantMock, dayMock, photoMock, reverseGeoMock, expenseMock, exchangeRateMock);

        // Act
        var result = await controller.Create();

        // Assert
        var vr = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<SpotCreateEditViewModel>(vr.Model);
        Assert.Equal("01:00", vm.DurationString);
        Assert.Equal(0, vm.Order);
    }

    [Fact]
    public async Task Create_Post_RedirectsToTripsDetails_WhenValid()
    {
        // Arrange
        var spotMock = new Mock<ISpotService>();
        var activityMock = new Mock<IActivityService>();
        var categoryMock = new Mock<IGenericService<Category>>();
        var tripMock = new Mock<ITripService>();
        var tripParticipantMock = new Mock<ITripParticipantService>();
        var dayMock = new Mock<IGenericService<Day>>();
        var photoMock = new Mock<IPhotoService>();
        var reverseGeoMock = new Mock<IReverseGeocodingService>();
        var expenseMock = new Mock<IExpenseService>();
        var exchangeRateMock = new Mock<IExchangeRateService>();

        spotMock.Setup(s => s.AddAsync(It.IsAny<Spot>())).ReturnsAsync((Spot s) => s);

        var controller = CreateController(spotMock, activityMock, categoryMock, tripMock, tripParticipantMock, dayMock, photoMock, reverseGeoMock, expenseMock, exchangeRateMock);

        var model = new SpotCreateEditViewModel
        {
            Name = "NewSpot",
            TripId = 123,
            DurationString = "01:00",
            Longitude = 1.0,
            Latitude = 2.0,
        };

        // Act
        var result = await controller.Create(model);

        // Assert
        var rr = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", rr.ActionName);
        Assert.Equal("Trips", rr.ControllerName);
        Assert.Equal(123, rr.RouteValues?["id"]);
        spotMock.Verify(s => s.AddAsync(It.Is<Spot>(sp => sp.Name == "NewSpot" && sp.TripId == 123)), Times.Once);
    }

    [Fact]
    public async Task Edit_Get_ReturnsForbid_WhenUserNotOwner()
    {
        // Arrange
        var spotMock = new Mock<ISpotService>();
        var activityMock = new Mock<IActivityService>();
        var categoryMock = new Mock<IGenericService<Category>>();
        var tripMock = new Mock<ITripService>();
        var tripParticipantMock = new Mock<ITripParticipantService>();
        var dayMock = new Mock<IGenericService<Day>>();
        var photoMock = new Mock<IPhotoService>();
        var reverseGeoMock = new Mock<IReverseGeocodingService>();
        var expenseMock = new Mock<IExpenseService>();
        var exchangeRateMock = new Mock<IExchangeRateService>();

        var trip = new Trip { Id = 20, PersonId = "ownerY", Name = "T" };
        var spot = new Spot { Id = 7, Name = "EditSpot", TripId = trip.Id, Trip = trip };

        spotMock.Setup(s => s.GetByIdAsync(spot.Id)).ReturnsAsync(spot);
        spotMock.Setup(s => s.UserOwnsSpotAsync(spot.Id, It.IsAny<string>())).ReturnsAsync(false);

        var controller = CreateController(spotMock, activityMock, categoryMock, tripMock, tripParticipantMock, dayMock, photoMock, reverseGeoMock, expenseMock, exchangeRateMock, currentUserId: "otherUser");

        // Act
        var result = await controller.Edit(spot.Id);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeleteConfirmed_RedirectsToTripsDetails_WhenDeleted()
    {
        // Arrange
        var spotMock = new Mock<ISpotService>();
        var activityMock = new Mock<IActivityService>();
        var categoryMock = new Mock<IGenericService<Category>>();
        var tripMock = new Mock<ITripService>();
        var tripParticipantMock = new Mock<ITripParticipantService>();
        var dayMock = new Mock<IGenericService<Day>>();
        var photoMock = new Mock<IPhotoService>();
        var reverseGeoMock = new Mock<IReverseGeocodingService>();
        var expenseMock = new Mock<IExpenseService>();
        var exchangeRateMock = new Mock<IExchangeRateService>();

        var trip = new Trip { Id = 30, Name = "TDel", PersonId = "u" };
        var spot = new Spot { Id = 8, Name = "ToDelete", TripId = trip.Id, Trip = trip, DayId = null };

        spotMock.Setup(s => s.GetByIdAsync(spot.Id)).ReturnsAsync(spot);
        spotMock.Setup(s => s.DeleteAsync(spot.Id)).Returns(Task.CompletedTask);

        var controller = CreateController(spotMock, activityMock, categoryMock, tripMock, tripParticipantMock, dayMock, photoMock, reverseGeoMock, expenseMock, exchangeRateMock);

        // Act
        var result = await controller.DeleteConfirmed(spot.Id);

        // Assert
        var rr = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", rr.ActionName);
        Assert.Equal("Trips", rr.ControllerName);
        Assert.Equal(trip.Id, rr.RouteValues?["id"]);
        spotMock.Verify(s => s.DeleteAsync(spot.Id), Times.Once);
    }
}
