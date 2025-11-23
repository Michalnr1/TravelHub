using Xunit;
using Moq;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Infrastructure.Services;

namespace TravelHub.Tests.Infrastructure.Tests.Services;

public class SpotServiceTests
{
    private readonly Mock<ISpotRepository> _mockSpotRepo;
    private readonly Mock<IActivityRepository> _mockActivityRepo;
    private readonly Mock<IGenericRepository<Country>> _mockCountryRepo;
    private readonly SpotService _sut;

    public SpotServiceTests()
    {
        _mockSpotRepo = new Mock<ISpotRepository>();
        _mockActivityRepo = new Mock<IActivityRepository>();
        _mockCountryRepo = new Mock<IGenericRepository<Country>>();
        _sut = new SpotService(_mockSpotRepo.Object, _mockActivityRepo.Object, _mockCountryRepo.Object);
    }

    [Fact]
    public async Task CalculateDailySpotCostAsync_ShouldCalculateCostOnlyForSpots()
    {
        // ARRANGE
        const int dayId = 10;
        var activities = new List<Activity>
        {
            // Spot 1
            // new Spot { Id = 1, Name = "Muzeum", Cost = 15.50m, DayId = dayId, Duration = 2m },
            new Spot { Id = 1, Name = "Muzeum", DayId = dayId, Duration = 2m },
            // Activity
            new Activity { Id = 2, Name = "Przejazd pociągiem", DayId = dayId, Duration = 1m },
            // Spot 2
            // new Spot { Id = 3, Name = "Wieża widokowa", Cost = 5.00m, DayId = dayId, Duration = 1.5m }
            new Spot { Id = 3, Name = "Wieża widokowa", DayId = dayId, Duration = 1.5m }
        };
        decimal expectedTotalCost = 20.50m;

        _mockActivityRepo
            .Setup(repo => repo.GetActivitiesByDayIdAsync(dayId))
            .ReturnsAsync(activities);

        // ACT
        var result = await _sut.CalculateDailySpotCostAsync(dayId);

        // ASSERT
        Assert.Equal(expectedTotalCost, result);
    }

    [Fact]
    public async Task CalculateDailySpotCostAsync_ShouldReturnZero_WhenNoSpotsAreFound()
    {
        // ARRANGE
        const int dayId = 20;
        var activities = new List<Activity>
        {
            new Activity { Id = 1, Name = "Śniadanie", DayId = dayId, Duration = 0.5m },
            new Activity { Id = 2, Name = "Kolacja", DayId = dayId, Duration = 1.0m }
        };

        _mockActivityRepo
            .Setup(repo => repo.GetActivitiesByDayIdAsync(dayId))
            .ReturnsAsync(activities);

        // ACT
        var result = await _sut.CalculateDailySpotCostAsync(dayId);

        // ASSERT
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task FindNearbySpotsAsync_ShouldCorrectlyFilterSpotsBasedOnHaversineDistance()
    {
        // ARRANGE
        const double centralLat = 52.2297; // Warszawa
        const double centralLon = 21.0122;
        const double radiusKm = 20.0;

        var allSpots = new List<Spot>
    {
        // Spot 1: W obrębie 20 km
        new Spot { Id = 1, Name = "Spot Blisko", Latitude = 52.285, Longitude = 21.135 },
        // Spot 2: Poza promieniem
        new Spot { Id = 2, Name = "Spot Daleko", Latitude = 51.758, Longitude = 19.456 },
        // Spot 3: Na granicy promienia (test dla precyzji)
        new Spot { Id = 3, Name = "Spot Granica", Latitude = 52.39, Longitude = 21.05 }
    };

        _mockSpotRepo
            .Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(allSpots);

        // ACT
        var nearbySpots = await _sut.FindNearbySpotsAsync(centralLat, centralLon, radiusKm);

        // ASSERT
        Assert.NotNull(nearbySpots);
        Assert.Equal(2, nearbySpots.Count);
        Assert.Contains(nearbySpots, s => s.Id == 1);
        Assert.Contains(nearbySpots, s => s.Id == 3);
        Assert.DoesNotContain(nearbySpots, s => s.Id == 2);
    }

    [Fact]
    public async Task FindNearbySpotsAsync_ShouldIgnoreSpotsWithZeroCoordinates()
    {
        // ARRANGE
        const double centralLat = 52.0;
        const double centralLon = 21.0;
        const double radiusKm = 100.0;

        var allSpots = new List<Spot>
    {
        new Spot { Id = 1, Name = "Prawidłowy", Latitude = 52.1, Longitude = 21.1 },
        new Spot { Id = 2, Name = "Zero", Latitude = 0.0, Longitude = 0.0 }
    };

        _mockSpotRepo
            .Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(allSpots);

        // ACT
        var nearbySpots = await _sut.FindNearbySpotsAsync(centralLat, centralLon, radiusKm);

        // ASSERT
        Assert.Single(nearbySpots);
        Assert.Contains(nearbySpots, s => s.Id == 1);
        Assert.DoesNotContain(nearbySpots, s => s.Id == 2);
    }
}