using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;

namespace TravelHub.Tests.TestUtilities;

public class FakeSpotRepository : ISpotRepository
{
    private readonly List<Spot> _spots = new();
    private readonly List<Transport> _transports = new();
    private int _nextSpotId = 1;
    private int _nextTransportId = 1;

    public Task<Spot?> GetByIdWithDetailsAsync(int id)
    {
        var spot = _spots.FirstOrDefault(s => s.Id == id);
        return Task.FromResult(spot);
    }

    public Task<IReadOnlyList<Spot>> GetSpotsUsedInTripTransportsAsync(int tripId)
    {
        var result = _spots.Where(s => s.TransportsFrom.Any(t => t.TripId == tripId) ||
                                       s.TransportsTo.Any(t => t.TripId == tripId))
                           .ToList();
        return Task.FromResult((IReadOnlyList<Spot>)result);
    }

    public Task<IReadOnlyList<Spot>> GetByTripIdAsync(int tripId)
    {
        var result = _spots.Where(s => s.TripId == tripId).ToList();
        return Task.FromResult((IReadOnlyList<Spot>)result);
    }

    public Task<IReadOnlyList<Spot>> GetTripSpotsWithDetailsAsync(int tripId)
    {
        var result = _spots.Where(s => s.TripId == tripId).ToList();
        return Task.FromResult((IReadOnlyList<Spot>)result);
    }

    public Task<IReadOnlyList<Spot>> GetAllWithDetailsAsync()
    {
        return Task.FromResult((IReadOnlyList<Spot>)_spots.ToList());
    }

    public Task<Spot?> GetByIdAsync(object id)
    {
        var intId = Convert.ToInt32(id);
        var spot = _spots.FirstOrDefault(s => s.Id == intId);
        return Task.FromResult(spot);
    }

    public Task<IReadOnlyList<Spot>> GetAllAsync()
    {
        return Task.FromResult((IReadOnlyList<Spot>)_spots.ToList());
    }

    public Task<Spot> AddAsync(Spot entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (entity.Id == 0) entity.Id = _nextSpotId++;
        entity.Photos ??= new List<Photo>();
        entity.TransportsFrom ??= new List<Transport>();
        entity.TransportsTo ??= new List<Transport>();
        _spots.Add(entity);
        return Task.FromResult(entity);
    }

    public Task UpdateAsync(Spot entity)
    {
        var existing = _spots.FirstOrDefault(s => s.Id == entity.Id);
        if (existing != null)
        {
            existing.Name = entity.Name;
            existing.Latitude = entity.Latitude;
            existing.Longitude = entity.Longitude;
            // existing.Cost = entity.Cost;
            existing.TripId = entity.TripId;
            existing.Trip = entity.Trip;
            existing.CategoryId = entity.CategoryId;
            existing.Category = entity.Category;
            existing.DayId = entity.DayId;
            existing.Day = entity.Day;
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Spot entity)
    {
        _spots.RemoveAll(s => s.Id == entity.Id);
        _transports.RemoveAll(t => t.FromSpotId == entity.Id || t.ToSpotId == entity.Id);
        return Task.CompletedTask;
    }

    // Helpers
    public Spot SeedSpot(Spot spot)
    {
        if (spot.Id == 0) spot.Id = _nextSpotId++;
        spot.Photos ??= new List<Photo>();
        spot.TransportsFrom ??= new List<Transport>();
        spot.TransportsTo ??= new List<Transport>();
        _spots.Add(spot);
        return spot;
    }

    public Transport SeedTransport(Transport t)
    {
        if (t.Id == 0) t.Id = _nextTransportId++;
        _transports.Add(t);
        var from = _spots.FirstOrDefault(s => s.Id == t.FromSpotId);
        var to = _spots.FirstOrDefault(s => s.Id == t.ToSpotId);
        if (from != null)
        {
            from.TransportsFrom.Add(t);
            t.FromSpot = from;
        }
        if (to != null)
        {
            to.TransportsTo.Add(t);
            t.ToSpot = to;
        }
        return t;
    }
}

public class FakeActivityRepository : IActivityRepository
{
    private readonly List<Activity> _activities = new();
    private int _nextId = 1;

    public Task<IReadOnlyList<Activity>> GetActivitiesByDayIdAsync(int dayId)
    {
        var result = _activities.Where(a => a.DayId == dayId).ToList();
        return Task.FromResult((IReadOnlyList<Activity>)result);
    }

    public Task<IReadOnlyList<Activity>> GetTripActivitiesWithDetailsAsync(int tripId)
    {
        var result = _activities.Where(a => a.TripId == tripId).ToList();
        return Task.FromResult((IReadOnlyList<Activity>)result);
    }

    public Task<IReadOnlyList<Activity>> GetAllWithDetailsAsync()
    {
        return Task.FromResult((IReadOnlyList<Activity>)_activities.ToList());
    }

    public Task<Activity?> GetByIdAsync(object id)
    {
        var intId = Convert.ToInt32(id);
        var a = _activities.FirstOrDefault(x => x.Id == intId);
        return Task.FromResult(a);
    }

    public Task<IReadOnlyList<Activity>> GetAllAsync()
    {
        return Task.FromResult((IReadOnlyList<Activity>)_activities.ToList());
    }

    public Task<Activity> AddAsync(Activity entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (entity.Id == 0) entity.Id = _nextId++;
        _activities.Add(entity);
        return Task.FromResult(entity);
    }

    public Task UpdateAsync(Activity entity)
    {
        var existing = _activities.FirstOrDefault(a => a.Id == entity.Id);
        if (existing != null)
        {
            existing.Name = entity.Name;
            existing.DayId = entity.DayId;
            existing.TripId = entity.TripId;
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Activity entity)
    {
        _activities.RemoveAll(a => a.Id == entity.Id);
        return Task.CompletedTask;
    }

    // Helper
    public Activity SeedActivity(Activity a)
    {
        if (a.Id == 0) a.Id = _nextId++;
        _activities.Add(a);
        return a;
    }
}