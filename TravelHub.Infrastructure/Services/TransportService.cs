using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;

namespace TravelHub.Infrastructure.Services;

public class TransportService : GenericService<Transport>, ITransportService
{
    private readonly ITransportRepository _transportRepository;

    public TransportService(ITransportRepository transportRepository) : base(transportRepository)
    {
        _transportRepository = transportRepository;
    }

    // Metody specyficzne dla Transportu:

    public async Task<IReadOnlyList<Transport>> GetTripTransportsWithDetailsAsync(int tripId)
    {
        return await _transportRepository.GetTransportsByTripIdAsync(tripId);
    }

    public async Task<decimal> CalculateTotalTravelDurationAsync(int tripId)
    {
        var transports = await _transportRepository.GetTransportsByTripIdAsync(tripId);

        if (transports == null || !transports.Any())
        {
            return 0;
        }

        return transports.Sum(t => t.Duration);
    }
}