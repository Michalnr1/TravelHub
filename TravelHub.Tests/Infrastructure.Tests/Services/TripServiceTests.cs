using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using TravelHub.Domain.DTOs;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Infrastructure.Services;
using Xunit;

namespace TravelHub.Tests.Infrastructure.Tests.Services;

public class TripServiceTests
{
    private (TripService sut,
             Mock<ITripRepository> tripRepo,
             Mock<IDayRepository> dayRepo,
             Mock<IAccommodationService> accService,
             Mock<ITripParticipantRepository> participantRepo,
             Mock<IBlogRepository> blogRepo,
             Mock<ILogger<TripService>> logger) CreateSut()
    {
        var tripRepo = new Mock<ITripRepository>();
        var dayRepo = new Mock<IDayRepository>();
        var accService = new Mock<IAccommodationService>();
        var participantRepo = new Mock<ITripParticipantRepository>();
        var blogRepo = new Mock<IBlogRepository>();
        var logger = new Mock<ILogger<TripService>>();
        var expenseRepo = new Mock<IExpenseRepository>();
        var exchangeRateRepo = new Mock<IExchangeRateRepository>();
        var currencyConversionService = new Mock<ICurrencyConversionService>();
        var categoryRepo = new Mock<ICategoryRepository>();
        var activityRepo = new Mock<IActivityRepository>();
        var spotRepo = new Mock<ISpotRepository>();
        var transportRepo = new Mock<ITransportRepository>();
        var participantService = new Mock<ITripParticipantService>();

        var sut = new TripService(tripRepo.Object, dayRepo.Object, activityRepo.Object, spotRepo.Object, accService.Object, transportRepo.Object, 
            expenseRepo.Object, exchangeRateRepo.Object, categoryRepo.Object, currencyConversionService.Object, participantRepo.Object, participantService.Object,
            blogRepo.Object, logger.Object);
        return (sut, tripRepo, dayRepo, accService, participantRepo, blogRepo, logger);
    }

    [Fact]
    public async Task AddDayToTripAsync_AddsDay_WhenValid()
    {
        var (sut, tripRepo, dayRepo, _, _, _, _) = CreateSut();
        var tripId = 1;
        var trip = new Trip { Id = tripId, Name = "Trip1", PersonId = "owner1", StartDate = new DateTime(2025,1,1), EndDate = new DateTime(2025,1,10) };

        tripRepo.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);
        dayRepo.Setup(r => r.GetByTripIdAsync(tripId)).ReturnsAsync(new List<Day>());

        var newDay = new Day { Date = new DateTime(2025,1,3), Number = 3 };

        var result = await sut.AddDayToTripAsync(tripId, newDay);

        Assert.Equal(tripId, result.TripId);
        dayRepo.Verify(r => r.AddAsync(newDay), Times.Once);
    }

    [Fact]
    public async Task AddDayToTripAsync_Throws_WhenTripNotFound()
    {
        var (sut, tripRepo, _, _, _, _, _) = CreateSut();
        tripRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Trip?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.AddDayToTripAsync(99, new Day { Date = DateTime.Today }));
    }

    [Fact]
    public async Task AddDayToTripAsync_Throws_WhenDateOutOfRange()
    {
        var (sut, tripRepo, _, _, _, _, _) = CreateSut();
        var tripId = 2;
        var trip = new Trip { Id = tripId, Name = "Trip2", PersonId = "owner2", StartDate = new DateTime(2025,1,1), EndDate = new DateTime(2025,1,5) };
        tripRepo.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);

        var invalidDay = new Day { Date = new DateTime(2025,2,1), Number = 10 };

        await Assert.ThrowsAsync<ArgumentException>(() => sut.AddDayToTripAsync(tripId, invalidDay));
    }

    [Fact]
    public async Task AddDayToTripAsync_Throws_WhenDuplicateNumber()
    {
        var (sut, tripRepo, dayRepo, _, _, _, _) = CreateSut();
        var tripId = 3;
        var trip = new Trip { Id = tripId, Name = "Trip3", PersonId = "owner3", StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(5) };
        tripRepo.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);

        var existing = new Day { Id = 1, Number = 2, TripId = tripId };
        dayRepo.Setup(r => r.GetByTripIdAsync(tripId)).ReturnsAsync(new List<Day> { existing });

        var newDay = new Day { Number = 2, Date = trip.StartDate.AddDays(1) };

        await Assert.ThrowsAsync<ArgumentException>(() => sut.AddDayToTripAsync(tripId, newDay));
    }

    [Fact]
    public async Task CreateNextDayAsync_AssignsAccommodation_AndReturnsNewDay()
    {
        var (sut, tripRepo, dayRepo, accService, _, _, _) = CreateSut();
        var tripId = 10;
        var trip = new Trip { Id = tripId, Name = "Trip10", PersonId = "owner10", StartDate = new DateTime(2025,1,1), EndDate = new DateTime(2025,1,10) };
        tripRepo.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);

        // existing days 1..2 so next should be 3
        dayRepo.Setup(r => r.GetByTripIdAsync(tripId)).ReturnsAsync(new List<Day>
        {
            new Day { Number = 1, Date = trip.StartDate },
            new Day { Number = 2, Date = trip.StartDate.AddDays(1) }
        });

        // accommodation that spans nextDayDate
        var accommodation = new Accommodation
        {
            Id = 100,
            Name = "Hotel A",
            TripId = tripId,
            CheckIn = trip.StartDate.AddDays(2),
            CheckOut = trip.StartDate.AddDays(5),
            DayId = null
        };
        accService.Setup(s => s.GetAccommodationByTripAsync(tripId)).ReturnsAsync(new List<Accommodation> { accommodation });
        accService.Setup(s => s.UpdateAsync(It.IsAny<Accommodation>())).Returns(Task.CompletedTask);
        dayRepo.Setup(r => r.AddAsync(It.IsAny<Day>())).ReturnsAsync((Day d) => { d.Id = 7; return d; });
        dayRepo.Setup(r => r.UpdateAsync(It.IsAny<Day>())).Returns(Task.CompletedTask);

        var result = await sut.CreateNextDayAsync(tripId);

        Assert.Equal(3, result.Number);
        Assert.Equal(tripId, result.TripId);
        accService.Verify(s => s.UpdateAsync(It.IsAny<Accommodation>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task CreateNextDayAsync_Throws_WhenNoDateAvailable()
    {
        var (sut, tripRepo, dayRepo, _, _, _, _) = CreateSut();
        var tripId = 20;
        var trip = new Trip { Id = tripId, Name = "Trip20", PersonId = "owner20", StartDate = new DateTime(2025,1,1), EndDate = new DateTime(2025,1,3) };

        // existing day numbers fill all trip days (1..3)
        dayRepo.Setup(r => r.GetByTripIdAsync(tripId)).ReturnsAsync(new List<Day>
        {
            new Day { Number = 1 }, new Day { Number = 2 }, new Day { Number = 3 }
        });
        tripRepo.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateNextDayAsync(tripId));
    }

    [Fact]
    public async Task GetMedianCoords_ComputesMediansFromTripSpots()
    {
        var (sut, tripRepo, _, _, _, _, _) = CreateSut();
        var tripId = 30;
        var trip = new Trip
        {
            Id = tripId,
            Name = "Trip30",
            PersonId = "owner30",
            Activities = new List<Activity>
            {
                new Spot { Id = 1, Name = "Spot1", TripId = tripId, Latitude = 10.0, Longitude = 20.0 },
                new Spot { Id = 2, Name = "Spot2", TripId = tripId, Latitude = 12.0, Longitude = 22.0 }
            },
            Days = new List<Day>
            {
                new Day
                {
                    Activities = new List<Activity>
                    {
                        new Spot { Id = 3, Name = "Spot3", TripId = tripId, Latitude = 14.0, Longitude = 24.0 }
                    }
                }
            }
        };

        tripRepo.Setup(r => r.GetByIdWithDaysAsync(tripId)).ReturnsAsync(trip);

        var (lat, lon) = await sut.GetMedianCoords(tripId);

        Assert.Equal(12.0, lat, 6);
        Assert.Equal(22.0, lon, 6);
    }

    [Fact]
    public async Task GetMedianCoords_Throws_WhenTripNotFound()
    {
        var (sut, tripRepo, _, _, _, _, _) = CreateSut();
        tripRepo.Setup(r => r.GetByIdWithDaysAsync(It.IsAny<int>())).ReturnsAsync((Trip?)null);

        await Assert.ThrowsAsync<ArgumentException>(() => sut.GetMedianCoords(999));
    }

    [Fact]
    public async Task ToggleChecklistItemAsync_TogglesExistingAndAddsNew()
    {
        var (sut, tripRepo, _, _, _, _, _) = CreateSut();
        var tripId = 40;
        var trip = new Trip
        {
            Id = tripId,
            Name = "Trip40",
            PersonId = "owner40",
            Checklist = new Checklist()
        };
        trip.Checklist.AddItem("one", false);

        tripRepo.Setup(r => r.GetByIdWithParticipantsAsync(tripId)).ReturnsAsync(trip);
        tripRepo.Setup(r => r.UpdateAsync(It.IsAny<Trip>())).Returns(Task.CompletedTask);

        // toggle existing
        await sut.ToggleChecklistItemAsync(tripId, "one");
        tripRepo.Verify(r => r.UpdateAsync(It.Is<Trip>(t => t.Checklist!.Items.Any(i => i.Title == "one"))), Times.Once);

        // add new
        await sut.ToggleChecklistItemAsync(tripId, "new-item");
        tripRepo.Verify(r => r.UpdateAsync(It.Is<Trip>(t => t.Checklist!.Items.Any(i => i.Title == "new-item" && i.IsCompleted))), Times.Exactly(2));
    }

    [Fact]
    public async Task RenameChecklistItemAsync_RenamesAndThrows()
    {
        // Poprawione deconstruct: tripRepoMock jest drugim elementem CreateSut (Mock<ITripRepository>)
        var (sut, tripRepoMock, _, _, _, _, _) = CreateSut();
        var tripId = 50;
        var trip = new Trip { Id = tripId, Name = "Trip50", PersonId = "owner50", Checklist = new Checklist() };
        trip.Checklist.AddItem("old", false);
        trip.Checklist.AddItem("existing", false);

        tripRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(tripId)).ReturnsAsync(trip);
        tripRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Trip>())).Returns(Task.CompletedTask);

        // successful rename
        await sut.RenameChecklistItemAsync(tripId, "old", "renamed");
        tripRepoMock.Verify(r => r.UpdateAsync(It.Is<Trip>(t => t.Checklist!.Items.Any(i => i.Title == "renamed"))), Times.Once);

        // attempt rename to already existing title -> throws
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RenameChecklistItemAsync(tripId, "renamed", "existing"));
    }

    [Fact]
    public async Task GetOrCreateBlogForTripAsync_CreatesBlog_WhenOwnerAndNoneExists()
    {
        var (sut, tripRepo, _, _, _, blogRepo, _) = CreateSut();
        var tripId = 60;
        var ownerId = "owner1";
        var trip = new Trip { Id = tripId, Name = "Trip60", PersonId = ownerId };

        tripRepo.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);
        blogRepo.Setup(r => r.GetByTripIdAsync(tripId)).ReturnsAsync((Blog?)null);
        blogRepo.Setup(r => r.AddAsync(It.IsAny<Blog>())).ReturnsAsync((Blog b) => { b.Id = 77; return b; });

        var result = await sut.GetOrCreateBlogForTripAsync(tripId, ownerId);

        Assert.NotNull(result);
        Assert.Equal(77, result!.Id);
        Assert.Equal(ownerId, result.OwnerId);
        blogRepo.Verify(r => r.AddAsync(It.IsAny<Blog>()), Times.Once);
    }

    [Fact]
    public async Task GetOrCreateBlogForTripAsync_ReturnsNull_WhenNotOwner()
    {
        var (sut, tripRepo, _, _, _, blogRepo, _) = CreateSut();
        var tripId = 61;
        var trip = new Trip { Id = tripId, Name = "Trip61", PersonId = "someoneElse" };

        tripRepo.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);
        var result = await sut.GetOrCreateBlogForTripAsync(tripId, "userX");

        Assert.Null(result);
        blogRepo.Verify(r => r.AddAsync(It.IsAny<Blog>()), Times.Never);
    }

    [Fact]
    public async Task HasBlogAsync_ReturnsTrue_IfBlogExists()
    {
        var (sut, _, _, _, _, blogRepo, _) = CreateSut();
        var tripId = 70;
        blogRepo.Setup(r => r.GetByTripIdAsync(tripId)).ReturnsAsync(new Blog { Id = 1, Name = "B", OwnerId = "owner", TripId = tripId });

        var result = await sut.HasBlogAsync(tripId);
        Assert.True(result);
    }

    [Fact]
    public void CountAllSpotsInTrip_ReturnsCorrectCount()
    {
        var (sut, _, _, _, _, _, _) = CreateSut();
        var trip = new Trip
        {
            Activities = new List<Activity> { new Spot { Id = 1, Name = "S1" }, new Activity { Id = 99, Name = "A1" } },
            Days = new List<Day>
            {
                new Day { Activities = new List<Activity> { new Spot { Id = 2, Name = "S2" }, new Spot { Id = 1, Name = "S1" } } }
            },
            Name = "Trip60",
            PersonId = "owner"
        };

        var count = sut.CountAllSpotsInTrip(trip);
        Assert.Equal(2, count);
    }

    [Fact]
    public void GetUniqueCountriesFromTrip_ReturnsSortedUnique()
    {
        var (sut, _, _, _, _, _, _) = CreateSut();
        var trip = new Trip
        {
            Activities = new List<Activity> { new Spot { CountryName = "Poland", Name = "S1", Id = 1 }, new Spot { CountryName = "USA", Name = "S2", Id = 2 } },
            Days = new List<Day> { new Day { Activities = new List<Activity> { new Spot { CountryName = "Poland", Name = "S3", Id = 3 } } } }
            ,
            Name = "Trip60",
            PersonId = "owner"
        };

        var countries = sut.GetUniqueCountriesFromTrip(trip);
        Assert.Equal(new List<string> { "Poland", "USA" }.OrderBy(x => x), countries.OrderBy(x => x));
    }

    [Fact]
    public async Task SearchPublicTripsAsync_FiltersByNameAndCountryAndDays()
    {
        var (sut, tripRepo, _, _, _, _, _) = CreateSut();
        var t1 = new Trip
        {
            Id = 1,
            Name = "Beach Holiday",
            PersonId = "ownerA",
            Person = new Person { Id = "ownerA", FirstName = "Owner", LastName = "A", IsPrivate = false, Nationality = "Poland" },
            StartDate = new DateTime(2025,1,1),
            EndDate = new DateTime(2025,1,3),
            Activities = new List<Activity> { new Spot { Id = 11, Name = "Beach", CountryCode = "PL" } }
        };
        var t2 = new Trip
        {
            Id = 2,
            Name = "Mountain Trip",
            PersonId = "ownerB",
            Person = new Person { Id = "ownerB", FirstName = "Owner", LastName = "B", IsPrivate = false, Nationality = "Poland" },
            StartDate = new DateTime(2025,1,1),
            EndDate = new DateTime(2025,1,10),
            Activities = new List<Activity> { new Spot { Id = 21, Name = "Peak", CountryCode = "DE" } }
        };

        tripRepo.Setup(r => r.GetPublicTripsAsync()).ReturnsAsync(new List<Trip> { t1, t2 });

        var criteria = new PublicTripSearchCriteriaDto { SearchTerm = "Beach", CountryCode = "PL", MinDays = 1, MaxDays = 5 };

        var results = (await sut.SearchPublicTripsAsync(criteria)).ToList();

        Assert.Single(results);
        Assert.Equal(t1.Id, results[0].Id);
    }

    [Fact]
    public async Task GetTripWithDetailsAsync_ReturnsValueFromRepository()
    {
        var (sut, tripRepo, _, _, _, _, _) = CreateSut();
        var trip = new Trip { Id = 101, Name = "DetailsTrip", PersonId = "p1" };
        tripRepo.Setup(r => r.GetByIdWithDaysAsync(trip.Id)).ReturnsAsync(trip);

        var result = await sut.GetTripWithDetailsAsync(trip.Id);

        Assert.Same(trip, result);
    }

    [Fact]
    public async Task GetTripDaysAsync_ReturnsDaysFromRepository()
    {
        var (sut, _, dayRepo, _, _, _, _) = CreateSut();
        var days = new List<Day> { new Day { Id = 1 }, new Day { Id = 2 } };
        dayRepo.Setup(d => d.GetByTripIdAsync(5)).ReturnsAsync(days);

        var result = await sut.GetTripDaysAsync(5);

        Assert.Equal(2, result.Count());
        Assert.Contains(result, d => d.Id == 1);
    }

    [Fact]
    public async Task UserOwnsTripAsync_ReturnsTrueAndFalse()
    {
        var (sut, tripRepo, _, _, _, _, _) = CreateSut();
        var trip = new Trip { Id = 7, Name = "test", PersonId = "owner7" };
        tripRepo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(trip);

        Assert.True(await sut.UserOwnsTripAsync(7, "owner7"));
        Assert.False(await sut.UserOwnsTripAsync(7, "someoneElse"));
    }

    [Fact]
    public async Task ExistsAsync_UsesRepository()
    {
        var (sut, tripRepo, _, _, _, _, _) = CreateSut();
        tripRepo.Setup(r => r.ExistsAsync(8)).ReturnsAsync(true);

        Assert.True(await sut.ExistsAsync(8));
    }

    [Fact]
    public void GetMedian_ReturnsZeroOnEmptyAndCorrectForEvenCount()
    {
        var (sut, _, _, _, _, _, _) = CreateSut();

        Assert.Equal(0, sut.GetMedian(Enumerable.Empty<double>()));

        var even = new[] { 1.0, 3.0, 5.0, 7.0 };
        var median = sut.GetMedian(even);
        Assert.Equal(4.0, median, 6);
    }

    [Fact]
    public async Task CreateNextDayAsync_PicksFirstMissingNumber_WhenGapAtStart()
    {
        var (sut, tripRepo, dayRepo, accService, _, _, _) = CreateSut();
        var tripId = 200;
        var trip = new Trip { Id = tripId, Name = "GappedTrip", PersonId = "o", StartDate = new DateTime(2025, 1, 1), EndDate = new DateTime(2025, 1, 5) };
        tripRepo.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync(trip);

        // existing days numbers start at 2 -> next should be 1
        dayRepo.Setup(d => d.GetByTripIdAsync(tripId)).ReturnsAsync(new List<Day> { new Day { Number = 2 }, new Day { Number = 3 } });
        dayRepo.Setup(d => d.AddAsync(It.IsAny<Day>())).ReturnsAsync((Day d) => { d.Id = 999; return d; });
        accService.Setup(a => a.GetAccommodationByTripAsync(tripId)).ReturnsAsync(Array.Empty<Accommodation>());

        var result = await sut.CreateNextDayAsync(tripId);

        Assert.Equal(1, result.Number);
        Assert.Equal(tripId, result.TripId);
    }

    [Fact]
    public async Task CreateNextDayAsync_Throws_WhenTripMissing()
    {
        var (sut, tripRepo, _, _, _, _, _) = CreateSut();
        tripRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Trip?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateNextDayAsync(1234));
    }

    [Fact]
    public async Task GetTripCurrencyAsync_ReturnsCurrencyOrThrows()
    {
        var (sut, tripRepo, _, _, _, _, _) = CreateSut();
        var trip = new Trip { Id = 300, Name = "test", CurrencyCode = CurrencyCode.EUR, PersonId = "p" };
        tripRepo.Setup(r => r.GetByIdAsync(trip.Id)).ReturnsAsync(trip);

        var currency = await sut.GetTripCurrencyAsync(trip.Id);
        Assert.Equal(CurrencyCode.EUR, currency);

        tripRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Trip?)null);
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetTripCurrencyAsync(999));
    }

    [Fact]
    public async Task GetAllTripParticipantsAsync_UsesRepository()
    {
        var (sut, _, _, _, participantRepo, _, _) = CreateSut();
        var participants = new List<Person> { new Person { Id = "p1", FirstName = "test", LastName = "B", IsPrivate = false, Nationality = "Poland" }, 
            new Person { Id = "p2", FirstName = "test", LastName = "B", IsPrivate = false, Nationality = "Poland" } };
        participantRepo.Setup(p => p.GetAllTripParticipantsAsync(10)).ReturnsAsync(participants);

        var res = await sut.GetAllTripParticipantsAsync(10);
        Assert.Equal(2, res.Count());
    }

    [Fact]
    public async Task GetChecklistAsync_ThrowsWhenTripMissing_OrReturnsEmpty()
    {
        var (sut, tripRepo, _, _, _, _, _) = CreateSut();
        tripRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Trip?)null);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.GetChecklistAsync(1));

        var trip = new Trip { Id = 2, Name = "test", Checklist = null, PersonId = "p" };
        tripRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(trip);
        var checklist = await sut.GetChecklistAsync(2);
        Assert.NotNull(checklist);
    }

    [Fact]
    public async Task AddChecklistItemAsync_AddsAndUpdatesRepository()
    {
        var (sut, tripRepo, _, _, _, _, _) = CreateSut();
        var trip = new Trip { Id = 12, Name = "test", Checklist = new Checklist(), PersonId = "p" };
        tripRepo.Setup(r => r.GetByIdAsync(trip.Id)).ReturnsAsync(trip);
        tripRepo.Setup(r => r.UpdateAsync(It.IsAny<Trip>())).Returns(Task.CompletedTask);

        await sut.AddChecklistItemAsync(trip.Id, "new-item");

        tripRepo.Verify(r => r.UpdateAsync(It.Is<Trip>(t => t.Checklist!.Items.Any(i => i.Title == "new-item"))), Times.Once);
    }

    [Fact]
    public async Task RemoveChecklistItemAsync_RemovesItemWhenPresent()
    {
        var (sut, tripRepo, _, _, _, _, _) = CreateSut();
        var trip = new Trip { Id = 13, Name = "test", Checklist = new Checklist(), PersonId = "p" };
        trip.Checklist.AddItem("to-remove", false);
        tripRepo.Setup(r => r.GetByIdAsync(trip.Id)).ReturnsAsync(trip);
        tripRepo.Setup(r => r.UpdateAsync(It.IsAny<Trip>())).Returns(Task.CompletedTask);

        await sut.RemoveChecklistItemAsync(trip.Id, "to-remove");

        tripRepo.Verify(r => r.UpdateAsync(It.Is<Trip>(t => !t.Checklist!.Items.Any(i => i.Title == "to-remove"))), Times.Once);
    }

    [Fact]
    public async Task ReplaceChecklistAsync_ReplacesAndUpdates()
    {
        var (sut, tripRepo, _, _, _, _, _) = CreateSut();
        var trip = new Trip { Id = 14, Name = "test", Checklist = new Checklist(), PersonId = "p" };
        tripRepo.Setup(r => r.GetByIdAsync(trip.Id)).ReturnsAsync(trip);
        tripRepo.Setup(r => r.UpdateAsync(It.IsAny<Trip>())).Returns(Task.CompletedTask);

        var newChecklist = new Checklist();
        newChecklist.AddItem("a", true);

        await sut.ReplaceChecklistAsync(trip.Id, newChecklist);

        tripRepo.Verify(r => r.UpdateAsync(It.Is<Trip>(t => t.Checklist!.Items.Any(i => i.Title == "a"))), Times.Once);
    }

    [Fact]
    public async Task AssignParticipantToItemAsync_AssignsAndUnassignsAndThrowsWhenMissing()
    {
        var (sut, tripRepo, _, _, _, _, _) = CreateSut();
        var tripId = 77;
        var trip = new Trip { Id = tripId, Name = "test", PersonId = "owner", Checklist = new Checklist() };
        trip.Checklist.AddItem("task1", false);

        var participant = new TripParticipant { Id = 55, PersonId = "p55", TripId = tripId, Status = TripParticipantStatus.Accepted };
        participant.Person = new Person { Id = "p55", FirstName = "John", LastName = "Doe", Nationality = "Poland", IsPrivate = false };
        trip.Participants.Add(participant);

        tripRepo.Setup(r => r.GetByIdWithParticipantsAsync(tripId)).ReturnsAsync(trip);
        tripRepo.Setup(r => r.UpdateAsync(It.IsAny<Trip>())).Returns(Task.CompletedTask);

        // assign by participant.Id
        await sut.AssignParticipantToItemAsync(tripId, "task1", "55");
        tripRepo.Verify(r => r.UpdateAsync(It.Is<Trip>(t => t.Checklist!.Items.Any(i => i.Title == "task1" && i.AssignedParticipantId == "55"))), Times.Once);

        // unassign
        await sut.AssignParticipantToItemAsync(tripId, "task1", null);
        tripRepo.Verify(r => r.UpdateAsync(It.Is<Trip>(t => t.Checklist!.Items.Any(i => i.Title == "task1" && i.AssignedParticipantId == null))), Times.AtLeastOnce);

        // invalid participant should throw
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.AssignParticipantToItemAsync(tripId, "task1", "999"));
    }

    [Fact]
    public async Task GetByIdWithParticipantsAsync_UsesRepository()
    {
        var (sut, tripRepo, _, _, _, _, _) = CreateSut();
        var trip = new Trip { Id = 88, Name = "test", PersonId = "p" };
        tripRepo.Setup(r => r.GetByIdWithParticipantsAsync(88)).ReturnsAsync(trip);

        var res = await sut.GetByIdWithParticipantsAsync(88);
        Assert.Same(trip, res);
    }

    [Fact]
    public async Task GetAvailableCountriesForPublicTripsAsync_UsesRepository()
    {
        var (sut, tripRepo, _, _, _, _, _) = CreateSut();
        var countries = new List<Country> { new Country { Code = "PL", Name = "Poland" } };
        tripRepo.Setup(r => r.GetCountriesForPublicTripsAsync()).ReturnsAsync(countries);

        var res = await sut.GetAvailableCountriesForPublicTripsAsync();
        Assert.Single(res);
    }
}
