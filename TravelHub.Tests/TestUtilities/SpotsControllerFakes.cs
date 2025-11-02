using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Services;

namespace TravelHub.Tests.TestUtilities;

// normalizer wymagany przez UserManager ctor
public class SimpleLookupNormalizer : ILookupNormalizer
{
    public string NormalizeEmail(string? email) => email?.ToUpperInvariant() ?? string.Empty;
    public string NormalizeName(string? name) => name?.ToUpperInvariant() ?? string.Empty;
}

// Minimalny IUserStore<T> aby zbudować UserManager w testach
public class FakeUserStore : IUserStore<Person>
{
    public Task<IdentityResult> CreateAsync(Person user, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<IdentityResult> DeleteAsync(Person user, CancellationToken cancellationToken) => throw new NotImplementedException();
    public void Dispose() { }
    public Task<Person?> FindByIdAsync(string userId, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<Person?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<string?> GetNormalizedUserNameAsync(Person user, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<string> GetUserIdAsync(Person user, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<string?> GetUserNameAsync(Person user, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task SetNormalizedUserNameAsync(Person user, string? normalizedName, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task SetUserNameAsync(Person user, string? userName, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<IdentityResult> UpdateAsync(Person user, CancellationToken cancellationToken) => throw new NotImplementedException();
}

// Factory tworząca UserManager<Person> użyteczny w testach
public static class TestUserManagerFactory
{
    public static UserManager<Person> Create()
    {
        var store = new FakeUserStore();
        var options = Options.Create(new IdentityOptions());
        var hasher = new PasswordHasher<Person>();
        var userValidators = Array.Empty<IUserValidator<Person>>();
        var pwdValidators = Array.Empty<IPasswordValidator<Person>>();
        var lookupNormalizer = new SimpleLookupNormalizer();
        var errors = new IdentityErrorDescriber();
        var services = (IServiceProvider?)null;
        var logger = NullLogger<UserManager<Person>>.Instance;

        return new UserManager<Person>(store, options, hasher, userValidators, pwdValidators, lookupNormalizer, errors, services, logger);
    }
}

// Prosty fake PhotoService
public class FakePhotoService : IPhotoService
{
    private readonly List<Photo> _photos = new();

    public Photo SeedPhoto(Photo p)
    {
        if (p.Id == 0) p.Id = _photos.Count + 1;
        _photos.Add(p);
        return p;
    }

    public Task<IReadOnlyList<Photo>> GetBySpotIdAsync(int spotId)
    {
        var result = _photos.Where(p => p.SpotId == spotId).ToList();
        return Task.FromResult((IReadOnlyList<Photo>)result);
    }

    // IGenericService<Photo> minimalne implementacje:
    public Task<Photo> AddAsync(Photo entity) => throw new NotImplementedException();
    public Task DeleteAsync(object id) => throw new NotImplementedException();
    public Task<IReadOnlyList<Photo>> GetAllAsync() => Task.FromResult((IReadOnlyList<Photo>)_photos.ToList());
    public Task<Photo> GetByIdAsync(object id) => Task.FromResult(_photos.FirstOrDefault(p => p.Id == (int)id));
    public Task UpdateAsync(Photo entity) => throw new NotImplementedException();

    public Task<Photo> AddPhotoAsync(Photo photo, Stream fileStream, string fileName, string webRootPath) => throw new NotImplementedException();
    public Task DeletePhotoAsync(int id, string webRootPath) => throw new NotImplementedException();
}

// Prosty fake TripService
public class FakeTripService : ITripService
{
    private readonly List<Trip> _trips = new();
    private int _nextId = 1;

    public Trip SeedTrip(Trip t)
    {
        if (t.Id == 0) t.Id = _nextId++;
        _trips.Add(t);
        return t;
    }

    public Task<Trip> GetByIdAsync(object id) => Task.FromResult(_trips.FirstOrDefault(t => t.Id == Convert.ToInt32(id)));

    public Task<IReadOnlyList<Trip>> GetAllAsync() => Task.FromResult((IReadOnlyList<Trip>)_trips.ToList());

    public Task<Trip> AddAsync(Trip entity)
    {
        if (entity.Id == 0) entity.Id = _nextId++;
        _trips.Add(entity);
        return Task.FromResult(entity);
    }

    public Task UpdateAsync(Trip entity)
    {
        var ex = _trips.FirstOrDefault(t => t.Id == entity.Id);
        if (ex != null)
        {
            ex.Name = entity.Name;
            ex.StartDate = entity.StartDate;
            ex.EndDate = entity.EndDate;
            ex.PersonId = entity.PersonId;
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(object id)
    {
        _trips.RemoveAll(t => t.Id == Convert.ToInt32(id));
        return Task.CompletedTask;
    }

    public Task<Trip?> GetTripWithDetailsAsync(int id) => GetByIdAsync(id);

    public Task<IEnumerable<Trip>> GetUserTripsAsync(string userId) => Task.FromResult((IEnumerable<Trip>)_trips.Where(t => t.PersonId == userId).ToList());

    public Task<Day> AddDayToTripAsync(int tripId, Day day) => throw new NotImplementedException();
    public Task<IEnumerable<Day>> GetTripDaysAsync(int tripId) => throw new NotImplementedException();

    public Task<bool> UserOwnsTripAsync(int tripId, string userId) => Task.FromResult(_trips.FirstOrDefault(t => t.Id == tripId)?.PersonId == userId);

    public Task<bool> ExistsAsync(int id) => Task.FromResult(_trips.Any(t => t.Id == id));

    public Task<(double medianLatitude, double medianLongitude)> GetMedianCoords(int id) => Task.FromResult((0.0, 0.0));

    public Task<IEnumerable<Trip>> GetAllWithUserAsync() => Task.FromResult((IEnumerable<Trip>)_trips.ToList());

    public Task<Day> CreateNextDayAsync(int tripId) => throw new NotImplementedException();

    public Task<CurrencyCode> GetTripCurrencyAsync(int tripId)
    {
        throw new NotImplementedException();
    }
}

// Minimalny fake Day service
public class FakeDayService : IGenericService<Day>
{
    private readonly List<Day> _days = new();
    public Day SeedDay(Day d)
    {
        if (d.Id == 0) d.Id = _days.Count + 1;
        _days.Add(d);
        return d;
    }

    public Task<Day> AddAsync(Day entity) => throw new NotImplementedException();
    public Task DeleteAsync(object id) { _days.RemoveAll(d => d.Id == Convert.ToInt32(id)); return Task.CompletedTask; }
    public Task<IReadOnlyList<Day>> GetAllAsync() => Task.FromResult((IReadOnlyList<Day>)_days.ToList());
    public Task<Day> GetByIdAsync(object id) => Task.FromResult(_days.FirstOrDefault(d => d.Id == Convert.ToInt32(id)));
    public Task UpdateAsync(Day entity) => throw new NotImplementedException();
}