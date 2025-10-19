using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;

namespace TravelHub.Infrastructure.Services;

public class DayService : GenericService<Day>, IDayService
{
    private readonly IDayRepository _dayRepository;

    public DayService(IDayRepository dayRepository)
        : base(dayRepository)
    {
        _dayRepository = dayRepository;
    }

    public async Task<Day?> GetDayWithDetailsAsync(int id)
    {
        return await _dayRepository.GetByIdWithActivitiesAsync(id);
    }
}
