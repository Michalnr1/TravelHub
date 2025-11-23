using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TravelHub.Domain.Entities;
using TravelHub.Domain.DTOs;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Tests.TestUtilities;
using TravelHub.Web.Controllers;
using TravelHub.Web.ViewModels.Trips;
using Xunit;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace TravelHub.Tests.Web.Tests.Controllers;

public class TripsControllerTests
{
    private static (TripsController controller,
                   Mock<ITripService> tripMock,
                   Mock<ITripParticipantService> tripParticipantMock,
                   Mock<ITransportService> transportMock,
                   Mock<ISpotService> spotMock,
                   Mock<IActivityService> activityMock,
                   Mock<ICategoryService> categoryMock,
                   Mock<IAccommodationService> accommodationMock,
                   Mock<IExpenseService> expenseMock,
                   Mock<IFlightService> flightMock,
                   Mock<IRecommendationService> recMock,
                   Mock<ILogger<TripsController>> loggerMock)
        CreateControllerWithMocks(string currentUserId = null)
    {
        var tripMock = new Mock<ITripService>();
        var tripParticipantMock = new Mock<ITripParticipantService>();
        var transportMock = new Mock<ITransportService>();
        var spotMock = new Mock<ISpotService>();
        var activityMock = new Mock<IActivityService>();
        var categoryMock = new Mock<ICategoryService>();
        var accommodationMock = new Mock<IAccommodationService>();
        var expenseMock = new Mock<IExpenseService>();
        var flightMock = new Mock<IFlightService>();
        var recMock = new Mock<IRecommendationService>();
        var loggerMock = new Mock<ILogger<TripsController>>();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[] { new KeyValuePair<string, string?>("ApiKeys:GoogleApiKey", "test") })
            .Build();

        var userManager = TestUserManagerFactory.Create();

        var controller = new TripsController(
            tripMock.Object,
            tripParticipantMock.Object,
            transportMock.Object,
            spotMock.Object,
            activityMock.Object,
            categoryMock.Object,
            accommodationMock.Object,
            expenseMock.Object,
            flightMock.Object,
            recMock.Object,
            loggerMock.Object,
            userManager,
            configuration);

        // prepare http context with optional user
        var httpContext = new DefaultHttpContext();
        if (!string.IsNullOrEmpty(currentUserId))
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, currentUserId) }, "TestAuth"));
        }
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // TempData is not required for these tests but set to avoid potential nulls
        controller.TempData = new TempDataDictionary(httpContext, new TestTempDataProvider());

        return (controller,
                tripMock,
                tripParticipantMock,
                transportMock,
                spotMock,
                activityMock,
                categoryMock,
                accommodationMock,
                expenseMock,
                flightMock,
                recMock,
                loggerMock);
    }

    [Fact]
    public async Task Index_ReturnsViewWithTasksOfViewModels()
    {
        var (controller, tripMock, tripParticipantMock, _, _, _, _, _, _, _, _, _) = CreateControllerWithMocks();

        var trip = new Trip
        {
            Id = 1,
            Name = "T1",
            PersonId = "u1",
            Person = new Person { Id = "u1", FirstName = "A", LastName = "B", IsPrivate = false, Nationality = "Poland" },
            Days = new List<Day> { new Day { Id = 1 } }
        };

        tripMock.Setup(t => t.GetAllWithUserAsync()).ReturnsAsync(new[] { trip });
        tripParticipantMock.Setup(p => p.GetParticipantCountAsync(trip.Id)).ReturnsAsync(3);

        var result = await controller.Index();

        var vr = Assert.IsType<ViewResult>(result);
        var modelTasks = Assert.IsAssignableFrom<IEnumerable<Task<TripWithUserViewModel>>>(vr.Model);
        var tasksList = modelTasks.ToList();
        Assert.Single(tasksList);

        // await the tasks to get the populated viewmodels
        var viewModels = await Task.WhenAll(tasksList);
        Assert.Equal(trip.Id, viewModels[0].Id);
        Assert.Equal(3, viewModels[0].ParticipantsCount);
    }

    [Fact]
    public async Task Details_ReturnsNotFound_WhenTripNull()
    {
        var (controller, tripMock, _, _, _, _, _, _, _, _, _, _) = CreateControllerWithMocks("user1");

        tripMock.Setup(t => t.GetTripWithDetailsAsync(It.IsAny<int>())).ReturnsAsync((Trip?)null);

        var result = await controller.Details(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Details_ReturnsForbid_WhenUserHasNoAccess()
    {
        var (controller, tripMock, tripParticipantMock, _, _, _, _, _, _, _, _, _) = CreateControllerWithMocks("user2");

        var trip = new Trip { Id = 5, Name = "test", PersonId = "ownerX", Person = new Person { Id = "ownerX", FirstName = "O", LastName = "X", IsPrivate = false, Nationality = "Poland" } };
        tripMock.Setup(t => t.GetTripWithDetailsAsync(trip.Id)).ReturnsAsync(trip);
        tripParticipantMock.Setup(tp => tp.UserHasAccessToTripAsync(trip.Id, It.IsAny<string>())).ReturnsAsync(false);

        var result = await controller.Details(trip.Id);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Create_Post_RedirectsToMyTrips_WhenValid()
    {
        var (controller, tripMock, tripParticipantMock, _, _, _, _, _, _, _, _, _) = CreateControllerWithMocks("creator1");

        var vm = new CreateTripViewModel
        {
            Name = "NewTrip",
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(2),
            IsPrivate = false,
            CurrencyCode = CurrencyCode.PLN
        };

        var createdTrip = new Trip { Id = 42, Name = vm.Name, PersonId = "creator1" };
        tripMock.Setup(t => t.AddAsync(It.IsAny<Trip>())).ReturnsAsync(createdTrip);
        tripParticipantMock.Setup(tp => tp.AddOwnerAsync(createdTrip.Id, It.IsAny<string>())).Returns(Task.FromResult<TripParticipant>(null));

        var result = await controller.Create(vm);

        var rr = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("MyTrips", rr.ActionName);
        tripMock.Verify(t => t.AddAsync(It.Is<Trip>(x => x.Name == vm.Name && x.PersonId == "creator1")), Times.Once);
        tripParticipantMock.Verify(tp => tp.AddOwnerAsync(createdTrip.Id, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task DeleteConfirmed_RedirectsToMyTrips_WhenDeleted()
    {
        var (controller, tripMock, _, _, _, _, _, _, _, _, _, _) = CreateControllerWithMocks("owner1");

        var trip = new Trip { Id = 10, PersonId = "owner1", Name = "TDel" };
        tripMock.Setup(t => t.GetByIdAsync(trip.Id)).ReturnsAsync(trip);
        tripMock.Setup(t => t.DeleteAsync(trip.Id)).Returns(Task.CompletedTask);

        var result = await controller.DeleteConfirmed(trip.Id);

        var rr = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("MyTrips", rr.ActionName);
        tripMock.Verify(t => t.DeleteAsync(trip.Id), Times.Once);
    }

    [Fact]
    public async Task GetTripCountries_ReturnsForbid_WhenUserNoAccess()
    {
        var (controller, _, tripParticipantMock, _, spotMock, _, _, _, _, _, _, _) = CreateControllerWithMocks("uX");

        tripParticipantMock.Setup(tp => tp.UserHasAccessToTripAsync(5, It.IsAny<string>())).ReturnsAsync(false);

        var result = await controller.GetTripCountries(5);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetTripCountries_ReturnsOkWithCountries_WhenUserHasAccess()
    {
        var (controller, _, tripParticipantMock, _, spotMock, _, _, _, _, _, _, _) = CreateControllerWithMocks("uY");

        tripParticipantMock.Setup(tp => tp.UserHasAccessToTripAsync(7, It.IsAny<string>())).ReturnsAsync(true);

        var countries = new List<Country>
        {
            new Country { Code = "PL", Name = "Poland", Spots = new List<Spot> { new Spot { Id = 1, Name = "test" } } },
            new Country { Code = "DE", Name = "Germany", Spots = new List<Spot>() }
        };

        spotMock.Setup(s => s.GetCountriesByTripAsync(7)).ReturnsAsync(countries);

        var result = await controller.GetTripCountries(7);

        var ok = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsAssignableFrom<List<CountryViewModel>>(ok.Value as IEnumerable<CountryViewModel> ?? ok.Value as List<CountryViewModel> ?? new List<CountryViewModel>());
        // we expect two items mapped
        Assert.Equal(2, returned.Count);
    }
}
