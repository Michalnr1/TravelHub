using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Repositories;

public interface ITripRepository
{
    Task<Trip?> GetByIdAsync(int id);
    Task<Trip?> GetByIdWithDaysAsync(int id);
    Task<IEnumerable<Trip>> GetByUserIdAsync(string userId);
    Task AddAsync(Trip trip);
    void Update(Trip trip);
    void Delete(Trip trip);
    Task<bool> ExistsAsync(int id);
}