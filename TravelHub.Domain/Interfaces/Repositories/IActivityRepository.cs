using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Repositories;

public interface IActivityRepository : IGenericRepository<Activity>
{
    // Metody specyficzne:

    // Pobiera wszystkie aktywności przypisane do konkretnego dnia w wycieczce
    Task<IReadOnlyList<Activity>> GetActivitiesByDayIdAsync(int dayId);

    // Pobiera aktywności w wycieczce z dodatkowymi danymi
    Task<IReadOnlyList<Activity>> GetTripActivitiesWithDetailsAsync(int tripId);
}
