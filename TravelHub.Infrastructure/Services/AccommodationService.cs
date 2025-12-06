using Microsoft.EntityFrameworkCore;
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

    public async Task<bool> HasDateConflictAsync(int tripId, DateTime checkIn, DateTime checkOut, int? excludeAccommodationId = null)
    {
        var accommodations = await _accommodationRepository.GetTripAccommodationsAsync(tripId);

        foreach (var accommodation in accommodations)
        {
            // Pomijamy obecne zakwaterowanie przy edycji
            if (excludeAccommodationId.HasValue && accommodation.Id == excludeAccommodationId.Value)
                continue;

            // Sprawdzamy nakładanie się zakresów dat
            // Zakwaterowanie nakłada się jeśli:
            // 1. Nowy checkIn jest w zakresie istniejącego zakwaterowania
            // 2. Nowy checkOut jest w zakresie istniejącego zakwaterowania
            // 3. Nowe zakwaterowanie zawiera całe istniejące zakwaterowanie

            bool newCheckInInExistingRange = checkIn >= accommodation.CheckIn && checkIn < accommodation.CheckOut;
            bool newCheckOutInExistingRange = checkOut > accommodation.CheckIn && checkOut <= accommodation.CheckOut;
            bool containsExisting = checkIn <= accommodation.CheckIn && checkOut >= accommodation.CheckOut;

            if (newCheckInInExistingRange || newCheckOutInExistingRange || containsExisting)
            {
                return true; // Znaleziono konflikt
            }
        }

        return false; // Brak konfliktów
    }
}
