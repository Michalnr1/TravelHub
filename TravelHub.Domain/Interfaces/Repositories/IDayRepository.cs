using System.Collections.Generic;
using System.Threading.Tasks;
using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Repositories;

public interface IDayRepository : IGenericRepository<Day>
{
    Task<Day?> GetByIdWithActivitiesAsync(int id);
    Task<IEnumerable<Day>> GetByTripIdAsync(int tripId);
    Task<bool> ExistsAsync(int id);
}