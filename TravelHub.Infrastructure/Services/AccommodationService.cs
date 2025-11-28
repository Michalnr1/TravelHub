using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;

namespace TravelHub.Infrastructure.Services;

public class AccommodationService : GenericService<Accommodation>, IAccommodationService
{
    private readonly IAccommodationRepository _accommodationRepository;

    public AccommodationService(IAccommodationRepository accommodationRepository)
        : base(accommodationRepository)
    {
        _accommodationRepository = accommodationRepository;
    }

    public async Task<Accommodation?> GetByIdWithDetailsAsync(int id)
    {
        return await _accommodationRepository.GetByIdWithDetailsAsync(id);
    }

    public async Task<IEnumerable<Accommodation>> GetAccommodationByTripAsync(int tripId)
    {
        return await _accommodationRepository.GetTripAccommodationsAsync(tripId);
    }

    public async Task<IReadOnlyList<Accommodation>> GetTripAccommodationsAsync(int tripId)
    {
        return await _accommodationRepository.GetTripAccommodationsAsync(tripId);
    }
}
