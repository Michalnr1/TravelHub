using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TravelHub.Infrastructure;
using TravelHub.Infrastructure.Repositories;
using TravelHub.Domain.Entities;
using Xunit;

namespace TravelHub.Tests.Repositories
{
    public class SpotRepositoryTests
    {
        private static ApplicationDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task GetByIdWithDetailsAsync_ReturnsSpotWithRelatedData()
        {
            var dbName = Guid.NewGuid().ToString();
            await using var context = CreateContext(dbName);

            var trip = new Trip
            {
                Name = "Trip1",
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(1),
                PersonId = "user",
                IsPrivate = false,
                Status = Status.Planning
            };
            context.Trips.Add(trip);
            await context.SaveChangesAsync();

            var category = new Category { Name = "Cat", Color = "#ABCDEF" };
            context.Categories.Add(category);
            await context.SaveChangesAsync();

            var day = new Day { Number = 1, Date = DateTime.Today, TripId = trip.Id };
            context.Days.Add(day);
            await context.SaveChangesAsync();

            var spot1 = new Spot
            {
                Name = "Spot1",
                TripId = trip.Id,
                CategoryId = category.Id,
                DayId = day.Id,
                Latitude = 10.1,
                Longitude = 20.2,
                // Cost = 12.5m
            };
            var spot2 = new Spot
            {
                Name = "Spot2",
                TripId = trip.Id
            };
            context.Activities.AddRange(spot1, spot2);
            await context.SaveChangesAsync();

            var photo = new Photo { Name = "photo1", SpotId = spot1.Id };
            context.Photos.Add(photo);
            await context.SaveChangesAsync();

            var transport1 = new Transport
            {
                Name = "T1",
                TripId = trip.Id,
                FromSpotId = spot1.Id,
                ToSpotId = spot2.Id,
                Type = TransportationType.Car,
                Duration = 1,
                // Cost = 0
            };
            var transport2 = new Transport
            {
                Name = "T2",
                TripId = trip.Id,
                FromSpotId = spot2.Id,
                ToSpotId = spot1.Id,
                Type = TransportationType.Bus,
                Duration = 2,
                // Cost = 0
            };
            context.Transports.AddRange(transport1, transport2);
            await context.SaveChangesAsync();

            var repo = new SpotRepository(context);
            var result = await repo.GetByIdWithDetailsAsync(spot1.Id);

            Assert.NotNull(result);
            Assert.Equal(spot1.Id, result!.Id);
            Assert.NotNull(result.Category);
            Assert.Equal(category.Id, result.Category!.Id);
            Assert.NotNull(result.Day);
            Assert.Equal(day.Id, result.Day!.Id);
            Assert.NotNull(result.Trip);
            Assert.Equal(trip.Id, result.Trip!.Id);
            Assert.NotNull(result.Photos);
            Assert.Contains(result.Photos, p => p.Id == photo.Id);
            Assert.NotNull(result.TransportsFrom);
            Assert.Contains(result.TransportsFrom, t => t.Id == transport1.Id);
            Assert.NotNull(result.TransportsTo);
            Assert.Contains(result.TransportsTo, t => t.Id == transport2.Id);
        }

        [Fact]
        public async Task GetSpotsUsedInTripTransportsAsync_ReturnsSpotsLinkedToGivenTrip()
        {
            var dbName = Guid.NewGuid().ToString();
            await using var context = CreateContext(dbName);

            var tripA = new Trip { Name = "A", StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(1), PersonId = "u", Status = Status.Planning, IsPrivate = false };
            var tripB = new Trip { Name = "B", StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(1), PersonId = "u", Status = Status.Planning, IsPrivate = false };
            context.Trips.AddRange(tripA, tripB);
            await context.SaveChangesAsync();

            var s1 = new Spot { Name = "S1", TripId = tripA.Id };
            var s2 = new Spot { Name = "S2", TripId = tripA.Id };
            var s3 = new Spot { Name = "S3", TripId = tripB.Id };
            context.Activities.AddRange(s1, s2, s3);
            await context.SaveChangesAsync();

            // transport for tripA connecting s1 <-> s2
            // var t1 = new Transport { Name = "TA1", TripId = tripA.Id, FromSpotId = s1.Id, ToSpotId = s2.Id, Type = TransportationType.Walk, Duration = 1, Cost = 0 };
            var t1 = new Transport { Name = "TA1", TripId = tripA.Id, FromSpotId = s1.Id, ToSpotId = s2.Id, Type = TransportationType.Walk, Duration = 1 };
            // transport for tripB connecting s3 <-> s1 (shouldn't be counted for tripA)
            // var t2 = new Transport { Name = "TB1", TripId = tripB.Id, FromSpotId = s3.Id, ToSpotId = s1.Id, Type = TransportationType.Bus, Duration = 1, Cost = 0 };
            var t2 = new Transport { Name = "TB1", TripId = tripB.Id, FromSpotId = s3.Id, ToSpotId = s1.Id, Type = TransportationType.Bus, Duration = 1 };
            context.Transports.AddRange(t1, t2);
            await context.SaveChangesAsync();

            var repo = new SpotRepository(context);
            var result = await repo.GetSpotsUsedInTripTransportsAsync(tripA.Id);

            var ids = result.Select(s => s.Id).ToList();
            Assert.Contains(s1.Id, ids);
            Assert.Contains(s2.Id, ids);
            Assert.DoesNotContain(s3.Id, ids);
            Assert.Equal(2, ids.Count);
        }

        [Fact]
        public async Task GetByTripIdAsync_ReturnsOnlySpotsForTrip()
        {
            var dbName = Guid.NewGuid().ToString();
            await using var context = CreateContext(dbName);

            var trip = new Trip { Name = "TripX", StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(1), PersonId = "u", Status = Status.Planning, IsPrivate = false };
            var other = new Trip { Name = "Other", StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(1), PersonId = "u", Status = Status.Planning, IsPrivate = false };
            context.Trips.AddRange(trip, other);
            await context.SaveChangesAsync();

            var s1 = new Spot { Name = "InTrip", TripId = trip.Id };
            var s2 = new Spot { Name = "OtherTrip", TripId = other.Id };
            context.Activities.AddRange(s1, s2);
            await context.SaveChangesAsync();

            var repo = new SpotRepository(context);
            var result = await repo.GetByTripIdAsync(trip.Id);

            Assert.Single(result);
            Assert.Equal(s1.Id, result.First().Id);
        }

        [Fact]
        public async Task GetTripSpotsWithDetailsAsync_IncludesNavigationProperties()
        {
            var dbName = Guid.NewGuid().ToString();
            await using var context = CreateContext(dbName);

            var trip = new Trip { Name = "T", StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(1), PersonId = "u", Status = Status.Planning, IsPrivate = false };
            context.Trips.Add(trip);
            await context.SaveChangesAsync();

            var cat = new Category { Name = "C", Color = "#000" };
            context.Categories.Add(cat);
            await context.SaveChangesAsync();

            var s = new Spot { Name = "WithDetails", TripId = trip.Id, CategoryId = cat.Id };
            context.Activities.Add(s);
            await context.SaveChangesAsync();

            var photo = new Photo { Name = "p", SpotId = s.Id };
            context.Photos.Add(photo);
            await context.SaveChangesAsync();

            var repo = new SpotRepository(context);
            var list = await repo.GetTripSpotsWithDetailsAsync(trip.Id);

            Assert.Single(list);
            var item = list.First();
            Assert.NotNull(item.Category);
            Assert.Equal(cat.Id, item.Category!.Id);
            Assert.NotNull(item.Photos);
            Assert.Contains(item.Photos, p => p.Id == photo.Id);
        }

        [Fact]
        public async Task GetAllWithDetailsAsync_ReturnsAllSpots()
        {
            var dbName = Guid.NewGuid().ToString();
            await using var context = CreateContext(dbName);

            var trip = new Trip { Name = "T1", StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(1), PersonId = "u", Status = Status.Planning, IsPrivate = false };
            context.Trips.Add(trip);
            await context.SaveChangesAsync();

            var s1 = new Spot { Name = "A", TripId = trip.Id };
            var s2 = new Spot { Name = "B", TripId = trip.Id };
            context.Activities.AddRange(s1, s2);
            await context.SaveChangesAsync();

            var repo = new SpotRepository(context);
            var all = await repo.GetAllWithDetailsAsync();

            Assert.True(all.Count >= 2);
            Assert.Contains(all, s => s.Id == s1.Id);
            Assert.Contains(all, s => s.Id == s2.Id);
        }

        [Fact]
        public async Task GetByIdAsync_Overridden_ReturnsSpotWithDetails()
        {
            var dbName = Guid.NewGuid().ToString();
            await using var context = CreateContext(dbName);

            var trip = new Trip { Name = "TripY", StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(1), PersonId = "u", Status = Status.Planning, IsPrivate = false };
            context.Trips.Add(trip);
            await context.SaveChangesAsync();

            var s = new Spot { Name = "Single", TripId = trip.Id };
            context.Activities.Add(s);
            await context.SaveChangesAsync();

            var repo = new SpotRepository(context);
            var found = await repo.GetByIdAsync(s.Id);

            Assert.NotNull(found);
            Assert.Equal(s.Id, found!.Id);
            Assert.NotNull(found.Trip);
            Assert.Equal(trip.Id, found.Trip!.Id);
        }

        [Fact]
        public async Task GetByIdWithDetailsAsync_ReturnsNull_WhenNotFound()
        {
            var dbName = Guid.NewGuid().ToString();
            await using var context = CreateContext(dbName);

            var repo = new SpotRepository(context);
            var result = await repo.GetByIdWithDetailsAsync(-12345);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
        {
            var dbName = Guid.NewGuid().ToString();
            await using var context = CreateContext(dbName);

            var repo = new SpotRepository(context);
            var result = await repo.GetByIdAsync(-999);

            Assert.Null(result);
        }
    }
}
