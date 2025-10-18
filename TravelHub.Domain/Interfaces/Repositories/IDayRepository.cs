using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Repositories;

public interface IDayRepository
{
    Task<Day?> GetByIdAsync(int id);
    Task<Day?> GetByIdWithActivitiesAsync(int id);
    Task<IEnumerable<Day>> GetByTripIdAsync(int tripId);
    Task AddAsync(Day day);
    void Update(Day day);
    void Delete(Day day);
    Task<bool> ExistsAsync(int id);
}
