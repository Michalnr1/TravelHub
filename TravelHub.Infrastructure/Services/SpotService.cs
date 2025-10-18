using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;

namespace TravelHub.Infrastructure.Services;

public class SpotService : GenericService<Spot>, ISpotService
{
    private readonly ISpotRepository _spotRepository;
    private readonly IActivityRepository _activityRepository;

    public SpotService(ISpotRepository spotRepository, IActivityRepository activityRepository)
        : base(spotRepository)
    {
        _spotRepository = spotRepository;
        _activityRepository = activityRepository;
    }

    public async Task<Spot?> GetSpotDetailsAsync(int id)
    {
        return await _spotRepository.GetByIdWithDetailsAsync(id);
    }

    public async Task<decimal> CalculateDailySpotCostAsync(int dayId)
    {
        var allActivitiesInDay = await _activityRepository.GetActivitiesByDayIdAsync(dayId);

        var spotsInDay = allActivitiesInDay
            .OfType<Spot>()
            .ToList();

        if (!spotsInDay.Any())
        {
            return 0;
        }

        return spotsInDay.Sum(s => s.Cost);
    }

    // Placeholder. It will need to get this info from Google API not from our DB.
    public async Task<IReadOnlyList<Spot>> FindNearbySpotsAsync(double latitude, double longitude, double radiusKm)
    {
        var allSpots = await _spotRepository.GetAllAsync();
        var nearbySpots = new List<Spot>();

        // Wzór Haversine do obliczania odległości
        const double R = 6371; // Promień Ziemi w kilometrach

        double latRad = ToRadians(latitude);
        double lonRad = ToRadians(longitude);

        foreach (var spot in allSpots)
        {
            if (spot.Latitude == 0 && spot.Longitude == 0) continue; // Pomijamy nieprawidłowe dane

            double spotLatRad = ToRadians(spot.Latitude);
            double spotLonRad = ToRadians(spot.Longitude);

            // Różnice w długościach i szerokościach
            double deltaLat = spotLatRad - latRad;
            double deltaLon = spotLonRad - lonRad;

            // Wzór Haversine
            double a = Math.Pow(Math.Sin(deltaLat / 2), 2) +
                       Math.Cos(latRad) * Math.Cos(spotLatRad) *
                       Math.Pow(Math.Sin(deltaLon / 2), 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            double distance = R * c; // Odległość w km

            if (distance <= radiusKm)
            {
                nearbySpots.Add(spot);
            }
        }

        return nearbySpots;
    }

    // Pomocnicza funkcja do konwersji stopni na radiany
    private double ToRadians(double angle)
    {
        return Math.PI * angle / 180.0;
    }
}