using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Services;

public interface IDayService : IGenericService<Day>
{
    Task<Day?> GetDayWithDetailsAsync(int id);
}
