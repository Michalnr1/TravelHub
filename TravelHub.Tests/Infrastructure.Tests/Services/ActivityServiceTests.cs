using Moq;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Infrastructure.Services;

namespace TravelHub.Tests.Infrastructure.Tests.Services;

public class ActivityServiceTests
{
    private readonly Mock<IActivityRepository> _mockActivityRepo;
    private readonly ActivityService _sut;

    // Konstruktor
    public ActivityServiceTests()
    {
        _mockActivityRepo = new Mock<IActivityRepository>();
        _sut = new ActivityService(_mockActivityRepo.Object);
    }

    [Fact]
    public async Task CalculateDailyActivityDurationAsync_ShouldReturnCorrectSum_WhenActivitiesExist()
    {
        // ARRANGE
        const int dayId = 1;
        var activities = new List<Activity>
        {
            new Activity { Id = 1, Duration = 2.5m, DayId = dayId, Name = "A1" },
            new Activity { Id = 2, Duration = 1.0m, DayId = dayId, Name = "A2" },
            new Activity { Id = 3, Duration = 3.5m, DayId = dayId, Name = "A3" }
        };
        decimal expectedDuration = 7.0m;

        _mockActivityRepo
            .Setup(repo => repo.GetActivitiesByDayIdAsync(dayId))
            .ReturnsAsync(activities);

        // ACT
        var result = await _sut.CalculateDailyActivityDurationAsync(dayId);

        // ASSERT
        Assert.Equal(expectedDuration, result);
    }

    [Fact]
    public async Task CalculateDailyActivityDurationAsync_ShouldReturnZero_WhenNoActivitiesExist()
    {
        // ARRANGE
        const int dayId = 2;
        var activities = new List<Activity>();

        _mockActivityRepo
            .Setup(repo => repo.GetActivitiesByDayIdAsync(dayId))
            .ReturnsAsync(activities);

        // ACT
        var result = await _sut.CalculateDailyActivityDurationAsync(dayId);

        // ASSERT
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ReorderActivitiesAsync_ShouldUpdateOrderAndCallRepositoryUpdate()
    {
        // ARRANGE
        const int dayId = 1;
        var activities = new List<Activity>
        {
            new Activity { Id = 10, Order = 1, DayId = dayId, Name = "Old A1" },
            new Activity { Id = 20, Order = 2, DayId = dayId, Name = "Old A2" },
            new Activity { Id = 30, Order = 3, DayId = dayId, Name = "Old A3" }
        };

        var orderUpdates = new List<(int activityId, int newOrder)>
        {
            (20, 1), // Change A2 to first position
            (10, 2)  // Change A1 to second position
        };

        _mockActivityRepo
            .Setup(repo => repo.GetActivitiesByDayIdAsync(dayId))
            .ReturnsAsync(activities);

        // ACT
        await _sut.ReorderActivitiesAsync(dayId, orderUpdates);

        // ASSERT
        // 1. Sprawdzenie, czy stan obiektów w pamięci został zmieniony
        Assert.Equal(2, activities.First(a => a.Id == 10).Order);
        Assert.Equal(1, activities.First(a => a.Id == 20).Order);

        // 2. Sprawdzenie, czy metoda UpdateAsync została wywołana dokładnie 2 razy
        _mockActivityRepo.Verify(repo => repo.UpdateAsync(It.IsAny<Activity>()), Times.Exactly(2));

        // 3. Sprawdzenie, czy metoda UpdateAsync została wywołana dla aktywności 10 z nowym polem Order=2
        _mockActivityRepo.Verify(
            repo => repo.UpdateAsync(It.Is<Activity>(a => a.Id == 10 && a.Order == 2)),
            Times.Once
        );

        // 4. Sprawdzenie, czy metoda UpdateAsync NIE została wywołana dla aktywności 30
        _mockActivityRepo.Verify(
            repo => repo.UpdateAsync(It.Is<Activity>(a => a.Id == 30)),
            Times.Never
        );
    }
}
