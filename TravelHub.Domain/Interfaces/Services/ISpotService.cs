using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Services;

public interface ISpotService : IGenericService<Spot>
{
    // Metody specyficzne:

    // Pobiera spot z pełnymi szczegółami (photos, transporty)
    Task<Spot?> GetSpotDetailsAsync(int id);

    // Znajduje wszystkie spoty w określonym promieniu
    Task<IReadOnlyList<Spot>> FindNearbySpotsAsync(double latitude, double longitude, double radiusKm);

    // Oblicza sumaryczny koszt wstępu do wszystkich spotów w danym dniu
    Task<decimal> CalculateDailySpotCostAsync(int dayId);

    // Pobiera wszystkie spoty dla danej podróży
    Task<IReadOnlyList<Spot>> GetSpotsByTripAsync(int tripId);

    Task<IEnumerable<Spot>> GetAllWithDetailsAsync();

    Task<IEnumerable<Spot>> GetTripSpotsWithDetailsAsync(int tripId);

    Task<bool> UserOwnsSpotAsync(int spotId, string userId);
    Task<Country> AddCountry(int spotId, string name, string code);
}
