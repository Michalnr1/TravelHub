using System;
using System.Linq;
using System.Threading.Tasks;
using TravelHub.Infrastructure.Services;
using TravelHub.Domain.Entities;
using TravelHub.Tests.TestUtilities;
using Xunit;

namespace TravelHub.Tests.Services;

public class SpotServiceTests
{
    [Fact]
    public async Task GetSpotDetailsAsync_ReturnsSpotWithDetails()
    {
        var spotRepo = new FakeSpotRepository();
        var activityRepo = new FakeActivityRepository();
        var service = new SpotService(spotRepo, activityRepo);

        var trip = new Trip { Id = 10, PersonId = "u1", Name = "Trip", StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(1), Status = Status.Planning, IsPrivate = false };
        var cat = new Category { Id = 2, Name = "C", Color = "#000" };
        var day = new Day { Id = 3, Number = 1, Date = DateTime.Today, TripId = trip.Id, Trip = trip };

        var spot = new Spot
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
            // Cost = 12.5m
        };

        spotRepo.SeedSpot(spot);

        var result = await service.GetSpotDetailsAsync(spot.Id);

        Assert.NotNull(result);
        Assert.Equal(spot.Id, result!.Id);
        Assert.NotNull(result.Trip);
        Assert.Equal(trip.Id, result.Trip!.Id);
        Assert.NotNull(result.Category);
        Assert.Equal(cat.Id, result.Category!.Id);
        Assert.NotNull(result.Day);
        Assert.Equal(day.Id, result.Day!.Id);
    }

    [Fact]
    public async Task CalculateDailySpotCostAsync_SumsCostOfSpotsInDay()
    {
        var spotRepo = new FakeSpotRepository();
        var activityRepo = new FakeActivityRepository();
        var service = new SpotService(spotRepo, activityRepo);

        int dayId = 42;
        // activityRepo.SeedActivity(new Spot { DayId = dayId, Cost = 5m, Name = "test" });
        activityRepo.SeedActivity(new Spot { DayId = dayId, Name = "test" });
        // activityRepo.SeedActivity(new Spot { DayId = dayId, Cost = 7.5m, Name = "test" });
        activityRepo.SeedActivity(new Spot { DayId = dayId, Name = "test" });
        // non-spot activity should be ignored
        activityRepo.SeedActivity(new Activity { DayId = dayId, Name = "test" });

        var sum = await service.CalculateDailySpotCostAsync(dayId);

        Assert.Equal(12.5m, sum);
    }

    [Fact]
    public async Task CalculateDailySpotCostAsync_ReturnsZero_WhenNoSpots()
    {
        var spotRepo = new FakeSpotRepository();
        var activityRepo = new FakeActivityRepository();
        var service = new SpotService(spotRepo, activityRepo);

        int dayId = 100;
        activityRepo.SeedActivity(new Activity { DayId = dayId, Name = "test" }); // only non-spot

        var sum = await service.CalculateDailySpotCostAsync(dayId);

        Assert.Equal(0m, sum);
    }

    [Fact]
    public async Task GetSpotsByTripAsync_DelegatesToRepository()
    {
        var spotRepo = new FakeSpotRepository();
        var activityRepo = new FakeActivityRepository();
        var service = new SpotService(spotRepo, activityRepo);

        var s1 = spotRepo.SeedSpot(new Spot { Name = "A", TripId = 1 });
        spotRepo.SeedSpot(new Spot { Name = "B", TripId = 2 });

        var list = await service.GetSpotsByTripAsync(1);

        Assert.Single(list);
        Assert.Equal(s1.Id, list.First().Id);
    }

    [Fact]
    public async Task GetAllWithDetailsAsync_ReturnsAll()
    {
        var spotRepo = new FakeSpotRepository();
        var activityRepo = new FakeActivityRepository();
        var service = new SpotService(spotRepo, activityRepo);

        var s1 = spotRepo.SeedSpot(new Spot { Name = "A" });
        var s2 = spotRepo.SeedSpot(new Spot { Name = "B" });

        var all = (await service.GetAllWithDetailsAsync()).ToList();

        Assert.True(all.Count >= 2);
        Assert.Contains(all, s => s.Id == s1.Id);
        Assert.Contains(all, s => s.Id == s2.Id);
    }

    [Fact]
    public async Task GetTripSpotsWithDetailsAsync_ReturnsTripSpots()
    {
        var spotRepo = new FakeSpotRepository();
        var activityRepo = new FakeActivityRepository();
        var service = new SpotService(spotRepo, activityRepo);

        var s1 = spotRepo.SeedSpot(new Spot { Name = "A", TripId = 7 });
        spotRepo.SeedSpot(new Spot { Name = "B", TripId = 8 });

        var list = (await service.GetTripSpotsWithDetailsAsync(7)).ToList();

        Assert.Single(list);
        Assert.Equal(s1.Id, list.First().Id);
    }

    [Fact]
    public async Task FindNearbySpotsAsync_ReturnsOnlySpotsWithinRadius()
    {
        var spotRepo = new FakeSpotRepository();
        var activityRepo = new FakeActivityRepository();
        var service = new SpotService(spotRepo, activityRepo);

        // center — mała niezerowa współrzędna, żeby nie być pominiętym przez serwis
        var centerSpot = spotRepo.SeedSpot(new Spot { Name = "C", Latitude = 0.0001, Longitude = 0.0 });
        // ~1.11 km north
        var nearSpot = spotRepo.SeedSpot(new Spot { Name = "N", Latitude = 0.01, Longitude = 0.0 });
        // far away (~157 km)
        spotRepo.SeedSpot(new Spot { Name = "F", Latitude = 1.0, Longitude = 1.0 });

        var found = (await service.FindNearbySpotsAsync(0.0, 0.0, 2.0)).ToList();

        Assert.Contains(found, s => s.Id == centerSpot.Id);
        Assert.Contains(found, s => s.Id == nearSpot.Id);
        Assert.DoesNotContain(found, s => s.Name == "F");
    }

    [Fact]
    public async Task UserOwnsSpotAsync_ReturnsTrue_WhenOwnerMatches()
    {
        var spotRepo = new FakeSpotRepository();
        var activityRepo = new FakeActivityRepository();
        var service = new SpotService(spotRepo, activityRepo);

        var trip = new Trip { Id = 55, PersonId = "owner", Name = "T", StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(1), Status = Status.Planning, IsPrivate = false };
        var spot = spotRepo.SeedSpot(new Spot { Name = "S", TripId = trip.Id, Trip = trip });

        var owns = await service.UserOwnsSpotAsync(spot.Id, "owner");
        Assert.True(owns);
    }

    [Fact]
    public async Task UserOwnsSpotAsync_ReturnsFalse_WhenOwnerDiffers()
    {
        var spotRepo = new FakeSpotRepository();
        var activityRepo = new FakeActivityRepository();
        var service = new SpotService(spotRepo, activityRepo);

        var trip = new Trip { Id = 66, PersonId = "owner2", Name = "T", StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(1), Status = Status.Planning, IsPrivate = false };
        var spot = spotRepo.SeedSpot(new Spot { Name = "S2", TripId = trip.Id, Trip = trip });

        var owns = await service.UserOwnsSpotAsync(spot.Id, "someoneElse");
        Assert.False(owns);
    }

    [Fact]
    public async Task UserOwnsSpotAsync_Throws_WhenSpotNotFound()
    {
        var spotRepo = new FakeSpotRepository();
        var activityRepo = new FakeActivityRepository();
        var service = new SpotService(spotRepo, activityRepo);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UserOwnsSpotAsync(9999, "u"));
    }
}
