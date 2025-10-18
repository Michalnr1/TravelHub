using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Repositories;

public interface IAccommodationRepository : IGenericRepository<Accommodation>
{
    // Metody specyficzne dla Zakwaterowania:

    // Pobiera wszystkie zakwaterowania związane z daną wycieczką.
    Task<IReadOnlyList<Accommodation>> GetTripAccommodationsAsync(int tripId);

    // Pobiera szczegółowe dane zakwaterowania, włączając powiązane Spoty
    Task<Accommodation?> GetByIdWithDetailsAsync(int id);
}
