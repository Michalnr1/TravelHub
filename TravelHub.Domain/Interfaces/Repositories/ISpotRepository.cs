using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Repositories;

public interface ISpotRepository : IGenericRepository<Spot>
{
    // Metody specyficzne dla Spot:

    // Pobiera spoty, które są punktami początkowymi lub końcowymi transportów w danej wycieczce.
    Task<IReadOnlyList<Spot>> GetSpotsUsedInTripTransportsAsync(int tripId);

    // Pobiera spoty wraz z kolekcjami
    Task<Spot?> GetByIdWithDetailsAsync(int id);

    // Pobiera wszystkie spoty dla danej podróży
    Task<IReadOnlyList<Spot>> GetByTripIdAsync(int tripId);

    Task<IReadOnlyList<Spot>> GetAllWithDetailsAsync();

    Task<IReadOnlyList<Spot>> GetTripSpotsWithDetailsAsync(int tripId);

    Task<IReadOnlyList<Country>> GetCountriesByTripAsync(int tripId);

    Task DeleteAsync(int id);
}