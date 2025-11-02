using System.Collections.Generic;
using System.Threading.Tasks;
using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Repositories;

public interface ITripRepository : IGenericRepository<Trip>
{
    Task<Trip?> GetByIdWithDaysAsync(int id);
    Task<IEnumerable<Trip>> GetByUserIdAsync(string userId);
    Task<bool> ExistsAsync(int id);
    Task<IReadOnlyList<Trip>> GetAllWithUserAsync();
    Task AddCountryToTrip(int id, Country country);
}