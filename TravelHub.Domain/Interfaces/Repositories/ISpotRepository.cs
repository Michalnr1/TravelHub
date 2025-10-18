using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Repositories;

public interface ISpotRepository : IGenericRepository<Spot>
{
    // Metody specyficzne dla Spot:

    // Pobiera spoty, które są punktami początkowymi lub końcowymi transportów w danej wycieczce.
    Task<IReadOnlyList<Spot>> GetSpotsUsedInTripTransportsAsync(int tripId);

    // Pobiera spoty wraz z kolekcjami
    Task<Spot?> GetByIdWithDetailsAsync(int id);
}