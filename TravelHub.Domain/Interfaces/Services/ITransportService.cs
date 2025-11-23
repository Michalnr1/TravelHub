using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Services;

public interface ITransportService : IGenericService<Transport>
{
    // Metody specyficzne dla Transportu:

    // Pobiera transporty dla wycieczki z uwzględnieniem danych From/To Spot
    Task<IReadOnlyList<Transport>> GetTripTransportsWithDetailsAsync(int tripId);

    // Oblicza sumaryczny czas trwania podróży
    Task<decimal> CalculateTotalTravelDurationAsync(int tripId);

    Task<Transport?> GetByIdWithDetailsAsync(int id);

    Task<IReadOnlyList<Transport>> GetAllWithDetailsAsync();

    Task<List<Transport>> GetTransportsFromSpotAsync(int spotId);
    Task<List<Transport>> GetTransportsToSpotAsync(int spotId);
}