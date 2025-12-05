using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;

namespace TravelHub.Infrastructure.Services;

public class FlightInfoService : IFlightInfoService
{
    private readonly IFlightInfoRepository _flightInfoRepository;

    public FlightInfoService(IFlightInfoRepository flightInfoRepository)
    {
        _flightInfoRepository = flightInfoRepository;
    }

    public async Task<FlightInfo?> GetByIdAsync(int id)
    {
        return await _flightInfoRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<FlightInfo>> GetByTripIdAsync(int tripId)
    {
        return await _flightInfoRepository.GetByTripIdAsync(tripId);
    }

    public async Task<IEnumerable<FlightInfo>> GetByTripAndUserAsync(int tripId, string userId)
    {
        return await _flightInfoRepository.GetByTripAndUserAsync(tripId, userId);
    }

    public async Task<FlightInfo> AddAsync(FlightInfo flightInfo)
    {
        flightInfo.AddedAt = DateTime.UtcNow;
        return await _flightInfoRepository.AddAsync(flightInfo);
    }

    public async Task UpdateAsync(FlightInfo flightInfo)
    {
        if (flightInfo.IsConfirmed && flightInfo.ConfirmedAt == null)
        {
            flightInfo.ConfirmedAt = DateTime.UtcNow;
        }
        await _flightInfoRepository.UpdateAsync(flightInfo);
    }

    public async Task DeleteAsync(int id)
    {
        var flightInfo = await GetByIdAsync(id);
        if (flightInfo != null)
        {
            await _flightInfoRepository.DeleteAsync(flightInfo);
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _flightInfoRepository.ExistsAsync(id);
    }

    public async Task ToggleConfirmationAsync(int id, bool isConfirmed)
    {
        var flightInfo = await GetByIdAsync(id);
        if (flightInfo != null)
        {
            flightInfo.IsConfirmed = isConfirmed;
            flightInfo.ConfirmedAt = isConfirmed ? DateTime.UtcNow : null;
            await UpdateAsync(flightInfo);
        }
    }

    public async Task<bool> UserCanModifyFlightAsync(int flightId, string userId)
    {
        var flightInfo = await GetByIdAsync(flightId);
        return flightInfo != null && flightInfo.PersonId == userId;
    }
}