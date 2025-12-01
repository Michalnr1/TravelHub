using Moq;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Infrastructure.Services;

namespace TravelHub.Tests.Infrastructure.Tests.Services
{
    public class DayServiceTests
    {
        private readonly Mock<IDayRepository> _dayRepositoryMock;
        private readonly Mock<ITripService> _tripServiceMock;
        private readonly Mock<IActivityService> _activityServiceMock;
        private readonly DayService _dayService;

        public DayServiceTests()
        {
            _dayRepositoryMock = new Mock<IDayRepository>();
            _tripServiceMock = new Mock<ITripService>();
            _activityServiceMock = new Mock<IActivityService>();

            _dayService = new DayService(
                _dayRepositoryMock.Object,
                _tripServiceMock.Object,
                _activityServiceMock.Object
            );
        }

        #region GetDayWithDetailsAsync Tests

        [Fact]
        public async Task GetDayWithDetailsAsync_WithExistingDay_ReturnsDayWithActivities()
        {
            // Arrange
            var dayId = 1;
            var expectedDay = new Day
            {
                Id = dayId,
                Number = 1,
                Activities = new List<Activity>
                {
                    new Spot { Id = 1, Name = "Test Spot" }
                }
            };

            _dayRepositoryMock
                .Setup(x => x.GetByIdWithActivitiesAsync(dayId))
                .ReturnsAsync(expectedDay);

            // Act
            var result = await _dayService.GetDayWithDetailsAsync(dayId);

            // Assert
            Assert.Equal(expectedDay, result);
            _dayRepositoryMock.Verify(x => x.GetByIdWithActivitiesAsync(dayId), Times.Once);
        }

        [Fact]
        public async Task GetDayWithDetailsAsync_WithNonExistingDay_ReturnsNull()
        {
            // Arrange
            var dayId = 999;

            _dayRepositoryMock
                .Setup(x => x.GetByIdWithActivitiesAsync(dayId))
                .ReturnsAsync((Day)null);

            // Act
            var result = await _dayService.GetDayWithDetailsAsync(dayId);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetDayByIdAsync Tests

        [Fact]
        public async Task GetDayByIdAsync_WithExistingDay_ReturnsDay()
        {
            // Arrange
            var dayId = 1;
            var expectedDay = new Day { Id = dayId, Number = 1 };

            _dayRepositoryMock
                .Setup(x => x.GetByIdAsync(dayId))
                .ReturnsAsync(expectedDay);

            // Act
            var result = await _dayService.GetDayByIdAsync(dayId);

            // Assert
            Assert.Equal(expectedDay, result);
            _dayRepositoryMock.Verify(x => x.GetByIdAsync(dayId), Times.Once);
        }

        [Fact]
        public async Task GetDayByIdAsync_WithNonExistingDay_ReturnsNull()
        {
            // Arrange
            var dayId = 999;

            _dayRepositoryMock
                .Setup(x => x.GetByIdAsync(dayId))
                .ReturnsAsync((Day)null);

            // Act
            var result = await _dayService.GetDayByIdAsync(dayId);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region UserOwnsDayAsync Tests

        [Fact]
        public async Task UserOwnsDayAsync_WithDayOwner_ReturnsTrue()
        {
            // Arrange
            var dayId = 1;
            var userId = "user1";
            var day = new Day
            {
                Id = dayId,
                Trip = new Trip { PersonId = userId, Name = "test" }
            };

            _dayRepositoryMock
                .Setup(x => x.GetByIdWithTripAsync(dayId))
                .ReturnsAsync(day);

            // Act
            var result = await _dayService.UserOwnsDayAsync(dayId, userId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UserOwnsDayAsync_WithDifferentUser_ReturnsFalse()
        {
            // Arrange
            var dayId = 1;
            var userId = "user1";
            var day = new Day
            {
                Id = dayId,
                Trip = new Trip { PersonId = "user2", Name = "test" } // Different user
            };

            _dayRepositoryMock
                .Setup(x => x.GetByIdWithTripAsync(dayId))
                .ReturnsAsync(day);

            // Act
            var result = await _dayService.UserOwnsDayAsync(dayId, userId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UserOwnsDayAsync_WithNonExistingDay_ReturnsFalse()
        {
            // Arrange
            var dayId = 999;
            var userId = "user1";

            _dayRepositoryMock
                .Setup(x => x.GetByIdWithTripAsync(dayId))
                .ReturnsAsync((Day)null);

            // Act
            var result = await _dayService.UserOwnsDayAsync(dayId, userId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UserOwnsDayAsync_WithDayWithoutTrip_ReturnsFalse()
        {
            // Arrange
            var dayId = 1;
            var userId = "user1";
            var day = new Day { Id = dayId, Trip = null };

            _dayRepositoryMock
                .Setup(x => x.GetByIdWithTripAsync(dayId))
                .ReturnsAsync(day);

            // Act
            var result = await _dayService.UserOwnsDayAsync(dayId, userId);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region IsDayAGroupAsync Tests

        [Fact]
        public async Task IsDayAGroupAsync_WithDayWithoutNumber_ReturnsTrue()
        {
            // Arrange
            var dayId = 1;
            var day = new Day { Id = dayId, Number = null }; // Group day

            _dayRepositoryMock
                .Setup(x => x.GetByIdAsync(dayId))
                .ReturnsAsync(day);

            // Act
            var result = await _dayService.IsDayAGroupAsync(dayId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsDayAGroupAsync_WithDayWithNumber_ReturnsFalse()
        {
            // Arrange
            var dayId = 1;
            var day = new Day { Id = dayId, Number = 1 }; // Normal day

            _dayRepositoryMock
                .Setup(x => x.GetByIdAsync(dayId))
                .ReturnsAsync(day);

            // Act
            var result = await _dayService.IsDayAGroupAsync(dayId);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region ValidateDateRangeAsync Tests

        [Fact]
        public async Task ValidateDateRangeAsync_WithDateInRange_ReturnsTrue()
        {
            // Arrange
            var tripId = 1;
            var date = new DateTime(2024, 6, 15);
            var trip = new Trip
            {
                Id = tripId,
                StartDate = new DateTime(2024, 6, 1),
                EndDate = new DateTime(2024, 6, 30), 
                Name = "test", PersonId = "test"
            };

            _tripServiceMock
                .Setup(x => x.GetByIdAsync(tripId))
                .ReturnsAsync(trip);

            // Act
            var result = await _dayService.ValidateDateRangeAsync(tripId, date);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ValidateDateRangeAsync_WithDateBeforeRange_ReturnsFalse()
        {
            // Arrange
            var tripId = 1;
            var date = new DateTime(2024, 5, 31);
            var trip = new Trip
            {
                Id = tripId,
                StartDate = new DateTime(2024, 6, 1),
                EndDate = new DateTime(2024, 6, 30),
                Name = "test",
                PersonId = "test"
            };

            _tripServiceMock
                .Setup(x => x.GetByIdAsync(tripId))
                .ReturnsAsync(trip);

            // Act
            var result = await _dayService.ValidateDateRangeAsync(tripId, date);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ValidateDateRangeAsync_WithDateAfterRange_ReturnsFalse()
        {
            // Arrange
            var tripId = 1;
            var date = new DateTime(2024, 7, 1);
            var trip = new Trip
            {
                Id = tripId,
                StartDate = new DateTime(2024, 6, 1),
                EndDate = new DateTime(2024, 6, 30),
                Name = "test",
                PersonId = "test"
            };

            _tripServiceMock
                .Setup(x => x.GetByIdAsync(tripId))
                .ReturnsAsync(trip);

            // Act
            var result = await _dayService.ValidateDateRangeAsync(tripId, date);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ValidateDateRangeAsync_WithNonExistingTrip_ReturnsFalse()
        {
            // Arrange
            var tripId = 999;
            var date = new DateTime(2024, 6, 15);

            _tripServiceMock
                .Setup(x => x.GetByIdAsync(tripId))
                .ReturnsAsync((Trip)null);

            // Act
            var result = await _dayService.ValidateDateRangeAsync(tripId, date);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ValidateDateRangeAsync_WithExactStartDate_ReturnsTrue()
        {
            // Arrange
            var tripId = 1;
            var startDate = new DateTime(2024, 6, 1);
            var trip = new Trip
            {
                Id = tripId,
                StartDate = startDate,
                EndDate = new DateTime(2024, 6, 30),
                Name = "test",
                PersonId = "test"
            };

            _tripServiceMock
                .Setup(x => x.GetByIdAsync(tripId))
                .ReturnsAsync(trip);

            // Act
            var result = await _dayService.ValidateDateRangeAsync(tripId, startDate);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ValidateDateRangeAsync_WithExactEndDate_ReturnsTrue()
        {
            // Arrange
            var tripId = 1;
            var endDate = new DateTime(2024, 6, 30);
            var trip = new Trip
            {
                Id = tripId,
                StartDate = new DateTime(2024, 6, 1),
                EndDate = endDate,
                Name = "test",
                PersonId = "test"
            };

            _tripServiceMock
                .Setup(x => x.GetByIdAsync(tripId))
                .ReturnsAsync(trip);

            // Act
            var result = await _dayService.ValidateDateRangeAsync(tripId, endDate);

            // Assert
            Assert.True(result);
        }

        #endregion

        #region GetDaysByTripIdAsync Tests

        [Fact]
        public async Task GetDaysByTripIdAsync_WithExistingTrip_ReturnsDays()
        {
            // Arrange
            var tripId = 1;
            var expectedDays = new List<Day>
            {
                new Day { Id = 1, TripId = tripId, Number = 1 },
                new Day { Id = 2, TripId = tripId, Number = 2 }
            };

            _dayRepositoryMock
                .Setup(x => x.GetByTripIdAsync(tripId))
                .ReturnsAsync(expectedDays);

            // Act
            var result = await _dayService.GetDaysByTripIdAsync(tripId);

            // Assert
            Assert.Equal(2, result.Count());
            Assert.All(result, day => Assert.Equal(tripId, day.TripId));
        }

        [Fact]
        public async Task GetDaysByTripIdAsync_WithNonExistingTrip_ReturnsEmptyList()
        {
            // Arrange
            var tripId = 999;

            _dayRepositoryMock
                .Setup(x => x.GetByTripIdAsync(tripId))
                .ReturnsAsync(new List<Day>());

            // Act
            var result = await _dayService.GetDaysByTripIdAsync(tripId);

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region AddAccommodationToDay Tests

        [Fact]
        public async Task AddAccommodationToDay_WithExistingDay_UpdatesAccommodation()
        {
            // Arrange
            var dayId = 1;
            var accommodationId = 10;
            var day = new Day { Id = dayId, AccommodationId = null };

            _dayRepositoryMock
                .Setup(x => x.GetByIdAsync(dayId))
                .ReturnsAsync(day);

            _dayRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Day>()))
                .Returns(Task.CompletedTask);

            // Act
            await _dayService.AddAccommodationToDay(dayId, accommodationId);

            // Assert
            _dayRepositoryMock.Verify(x => x.GetByIdAsync(dayId), Times.Once);
            _dayRepositoryMock.Verify(x => x.UpdateAsync(It.Is<Day>(d =>
                d.Id == dayId && d.AccommodationId == accommodationId)), Times.Once);
        }

        [Fact]
        public async Task AddAccommodationToDay_WithNonExistingDay_ThrowsException()
        {
            // Arrange
            var dayId = 999;
            var accommodationId = 10;

            _dayRepositoryMock
                .Setup(x => x.GetByIdAsync(dayId))
                .ReturnsAsync((Day)null);

            // Act & Assert
            await Assert.ThrowsAsync<NullReferenceException>(() =>
                _dayService.AddAccommodationToDay(dayId, accommodationId));
        }

        #endregion

        #region GetMedianCoords Tests

        [Fact]
        public async Task GetMedianCoords_WithDayAndSpots_ReturnsCorrectMedians()
        {
            // Arrange
            var dayId = 1;
            var day = new Day
            {
                Id = dayId,
                TripId = 1,
                Activities = new List<Activity>
                {
                    new Spot { Id = 1, Latitude = 50.0, Longitude = 20.0, Name = "test" },
                    new Spot { Id = 2, Latitude = 52.0, Longitude = 22.0, Name = "test" },
                    new Spot { Id = 3, Latitude = 54.0, Longitude = 24.0, Name = "test" }
                }
            };

            _dayRepositoryMock
                .Setup(x => x.GetByIdWithActivitiesAsync(dayId))
                .ReturnsAsync(day);

            _tripServiceMock
                .Setup(x => x.GetTripWithDetailsAsync(day.TripId))
                .ReturnsAsync((Trip)null);

            // Act
            var result = await _dayService.GetMedianCoords(dayId);

            // Assert
            // Median of [50.0, 52.0, 54.0] is 52.0
            // Median of [20.0, 22.0, 24.0] is 22.0
            Assert.Equal(52.0, result.medianLatitude);
            Assert.Equal(22.0, result.medianLongitude);
        }

        [Fact]
        public async Task GetMedianCoords_WithNonExistingDay_ThrowsArgumentException()
        {
            // Arrange
            var dayId = 999;

            _dayRepositoryMock
                .Setup(x => x.GetByIdWithActivitiesAsync(dayId))
                .ReturnsAsync((Day)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _dayService.GetMedianCoords(dayId));
        }

        [Fact]
        public async Task GetMedianCoords_WithAccommodationAndPreviousDay_IncludesBoth()
        {
            // Arrange
            var dayId = 2;
            var tripId = 1;
            var day = new Day
            {
                Id = dayId,
                TripId = tripId,
                Number = 2,
                Accommodation = new Accommodation { Id = 1, Latitude = 51.0, Longitude = 21.0, Name = "test" },
                Activities = new List<Activity>
                {
                    new Spot { Id = 1, Latitude = 50.0, Longitude = 20.0, Name = "test" }
                }
            };

            var trip = new Trip
            {
                Id = tripId,
                Days = new List<Day>
                {
                    new Day { Id = 1, Number = 1, Accommodation = new Accommodation { Id = 2, Latitude = 49.0, Longitude = 19.0, Name = "test" } },
                    day
                },
                Name = "test",
                PersonId = "test"
            };

            _dayRepositoryMock
                .Setup(x => x.GetByIdWithActivitiesAsync(dayId))
                .ReturnsAsync(day);

            _tripServiceMock
                .Setup(x => x.GetTripWithDetailsAsync(tripId))
                .ReturnsAsync(trip);

            // Act
            var result = await _dayService.GetMedianCoords(dayId);

            // Assert
            // All spots: [50.0, 51.0, 49.0] -> ordered: [49.0, 50.0, 51.0] -> median: 50.0
            // Longitudes: [20.0, 21.0, 19.0] -> ordered: [19.0, 20.0, 21.0] -> median: 20.0
            Assert.Equal(50.0, result.medianLatitude);
            Assert.Equal(20.0, result.medianLongitude);
        }

        [Fact]
        public async Task GetMedianCoords_WithNoSpots_ReturnsZero()
        {
            // Arrange
            var dayId = 1;
            var day = new Day
            {
                Id = dayId,
                TripId = 1,
                Activities = new List<Activity>() // No activities
            };

            _dayRepositoryMock
                .Setup(x => x.GetByIdWithActivitiesAsync(dayId))
                .ReturnsAsync(day);

            _tripServiceMock
                .Setup(x => x.GetTripWithDetailsAsync(day.TripId))
                .ReturnsAsync((Trip)null);

            // Act
            var result = await _dayService.GetMedianCoords(dayId);

            // Assert
            Assert.Equal(0, result.medianLatitude);
            Assert.Equal(0, result.medianLongitude);
        }

        #endregion

        #region GetMedian Tests

        [Theory]
        [InlineData(new double[] { 1, 2, 3 }, 2.0)] // Odd count
        [InlineData(new double[] { 1, 2, 3, 4 }, 2.5)] // Even count
        [InlineData(new double[] { 5 }, 5.0)] // Single element
        [InlineData(new double[] { 10, 20 }, 15.0)] // Two elements
        public void GetMedian_WithNumbers_ReturnsCorrectMedian(double[] numbers, double expectedMedian)
        {
            // Act
            var result = _dayService.GetMedian(numbers);

            // Assert
            Assert.Equal(expectedMedian, result);
        }

        [Fact]
        public void GetMedian_WithEmptyCollection_ReturnsZero()
        {
            // Arrange
            var numbers = Enumerable.Empty<double>();

            // Act
            var result = _dayService.GetMedian(numbers);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void GetMedian_WithNullCollection_ReturnsZero()
        {
            // Act
            var result = _dayService.GetMedian(null);

            // Assert
            Assert.Equal(0, result);
        }

        #endregion

        #region CheckNewForCollisions Tests

        [Fact]
        public async Task CheckNewForCollisions_WithCollision_ReturnsCollidingActivity()
        {
            // Arrange
            var dayId = 1;
            var startTimeString = "10:00";
            var durationString = "2:00"; // Ends at 12:00
            var activities = new List<Activity>
            {
                new Activity { Id = 1, StartTime = 9.0m, Duration = 2.0m, Name = "test" }, // 9:00-11:00
                new Activity { Id = 2, StartTime = 11.5m, Duration = 1.0m, Name = "test" } // 11:30-12:30
            };

            _activityServiceMock
                .Setup(x => x.GetOrderedDailyActivitiesAsync(dayId))
                .ReturnsAsync(activities);

            // Act
            var result = await _dayService.CheckNewForCollisions(dayId, startTimeString, durationString);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id); // Should collide with first activity (10:00-12:00 overlaps with 9:00-11:00)
        }

        [Fact]
        public async Task CheckNewForCollisions_WithoutCollision_ReturnsNull()
        {
            // Arrange
            var dayId = 1;
            var startTimeString = "14:00";
            var durationString = "1:00"; // Ends at 15:00
            var activities = new List<Activity>
            {
                new Activity { Id = 1, StartTime = 10.0m, Duration = 2.0m, Name = "test" }, // 10:00-12:00
                new Activity { Id = 2, StartTime = 12.0m, Duration = 1.0m, Name = "test" }  // 12:00-13:00
            };

            _activityServiceMock
                .Setup(x => x.GetOrderedDailyActivitiesAsync(dayId))
                .ReturnsAsync(activities);

            // Act
            var result = await _dayService.CheckNewForCollisions(dayId, startTimeString, durationString);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CheckNewForCollisions_WithEmptyDuration_ReturnsCollision()
        {
            // Arrange
            var dayId = 1;
            var startTimeString = "10:00";
            string durationString = "00:00"; // Zero duration
            var activity = new Activity { Id = 1, StartTime = 10.0m, Duration = 2.0m, Name = "test" }; 
            var activities = new List<Activity>
            {
                activity
            };

            _activityServiceMock
                .Setup(x => x.GetOrderedDailyActivitiesAsync(dayId))
                .ReturnsAsync(activities);

            // Act
            var result = await _dayService.CheckNewForCollisions(dayId, startTimeString, durationString);

            // Assert
            Assert.Equal(activity, result);
        }

        #endregion

        #region CheckAllForCollisions Tests

        [Fact]
        public async Task CheckAllForCollisions_WithCollidingActivities_ReturnsFirstCollision()
        {
            // Arrange
            var dayId = 1;
            var activities = new List<Activity>
            {
                new Activity { Id = 1, StartTime = 10.0m, Duration = 2.0m, Name = "test" }, // 10:00-12:00
                new Activity { Id = 2, StartTime = 11.0m, Duration = 2.0m , Name = "test"}, // 11:00-13:00 - collides with first
                new Activity { Id = 3, StartTime = 14.0m, Duration = 1.0m , Name = "test"}  // 14:00-15:00
            };

            _activityServiceMock
                .Setup(x => x.GetOrderedDailyActivitiesAsync(dayId))
                .ReturnsAsync(activities);

            // Act
            var result = await _dayService.CheckAllForCollisions(dayId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Value.Item1.Id); // First activity in collision
            Assert.Equal(2, result.Value.Item2.Id); // Second activity in collision
        }

        [Fact]
        public async Task CheckAllForCollisions_WithoutCollisions_ReturnsNull()
        {
            // Arrange
            var dayId = 1;
            var activities = new List<Activity>
            {
                new Activity { Id = 1, StartTime = 10.0m, Duration = 1.0m , Name = "test"}, // 10:00-11:00
                new Activity { Id = 2, StartTime = 12.0m, Duration = 1.0m , Name = "test"}, // 12:00-13:00
                new Activity { Id = 3, StartTime = 14.0m, Duration = 1.0m , Name = "test"}  // 14:00-15:00
            };

            _activityServiceMock
                .Setup(x => x.GetOrderedDailyActivitiesAsync(dayId))
                .ReturnsAsync(activities);

            // Act
            var result = await _dayService.CheckAllForCollisions(dayId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CheckAllForCollisions_WithActivitiesWithoutStartTime_IgnoresThem()
        {
            // Arrange
            var dayId = 1;
            var activities = new List<Activity>
            {
                new Activity { Id = 1, StartTime = null, Duration = 2.0m, Name = "test" }, // No start time - ignored
                new Activity { Id = 2, StartTime = 5.0m, Duration = 1.0m , Name = "test"},
                new Activity { Id = 3, StartTime = 11.0m, Duration = 1.0m , Name = "test"   } // No collision
            };

            _activityServiceMock
                .Setup(x => x.GetOrderedDailyActivitiesAsync(dayId))
                .ReturnsAsync(activities);

            // Act
            var result = await _dayService.CheckAllForCollisions(dayId);

            // Assert
            Assert.Null(result); // No collisions among activities with start times
        }

        [Fact]
        public async Task CheckAllForCollisions_WithEmptyActivities_ReturnsNull()
        {
            // Arrange
            var dayId = 1;
            var activities = new List<Activity>();

            _activityServiceMock
                .Setup(x => x.GetOrderedDailyActivitiesAsync(dayId))
                .ReturnsAsync(activities);

            // Act
            var result = await _dayService.CheckAllForCollisions(dayId);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region ConvertTimeStringToDecimal Tests (Private Method)

        [Theory]
        [InlineData("10:30", 10.5)]    // 10 hours 30 minutes
        [InlineData("8:15", 8.25)]     // 8 hours 15 minutes  
        [InlineData("0:45", 0.75)]     // 45 minutes
        [InlineData("24:00", 24.0)]    // 24 hours
        public void ConvertTimeStringToDecimal_WithValidTime_ReturnsCorrectDecimal(string timeString, decimal expected)
        {
            // Use reflection to test private method
            var method = typeof(DayService).GetMethod("ConvertTimeStringToDecimal",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var result = (decimal)method.Invoke(_dayService, new object[] { timeString });

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("invalid")]
        [InlineData("10")]
        [InlineData("10:")]
        [InlineData(":30")]
        [InlineData("abc:def")]
        public void ConvertTimeStringToDecimal_WithInvalidTime_ReturnsZero(string timeString)
        {
            // Use reflection to test private method
            var method = typeof(DayService).GetMethod("ConvertTimeStringToDecimal",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var result = (decimal)method.Invoke(_dayService, new object[] { timeString });

            // Assert
            Assert.Equal(0, result);
        }

        #endregion
    }
}