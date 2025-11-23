using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Repositories;

public interface ITransportRepository : IGenericRepository<Transport>
{
    // Metody specyficzne dla Transportu:

    // Pobiera wszystkie transporty związane z daną wycieczką.
    Task<IReadOnlyList<Transport>> GetTransportsByTripIdAsync(int tripId);

    // Można dodać: Task<IReadOnlyList<Transport>> GetTransportsByTypeAsync(TransportationType type);

    Task<Transport?> GetByIdWithDetailsAsync(int id);

    Task<IReadOnlyList<Transport>> GetAllWithDetailsAsync();

    Task<List<Transport>> GetTransportsFromSpotAsync(int spotId);
    Task<List<Transport>> GetTransportsToSpotAsync(int spotId);
}
