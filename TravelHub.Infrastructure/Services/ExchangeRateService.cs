using Microsoft.EntityFrameworkCore;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;

namespace TravelHub.Infrastructure.Services;

public class ExchangeRateService : GenericService<ExchangeRate>, IExchangeRateService
{
    private readonly IExchangeRateRepository _exchangeRateRepository;

    public ExchangeRateService(IExchangeRateRepository exchangeRateRepository)
        : base(exchangeRateRepository)
    {
        _exchangeRateRepository = exchangeRateRepository;
    }

    public async Task<IReadOnlyList<ExchangeRate>> GetTripExchangeRatesAsync(int tripId)
    {
        return await _exchangeRateRepository.GetTripExchangeRatesAsync(tripId);
    }

    public async Task<ExchangeRate> GetOrCreateExchangeRateAsync(int tripId, CurrencyCode code, decimal rate)
    {
        var existingRate = await _exchangeRateRepository.GetExistingRateAsync(tripId, code, rate);

        if (existingRate != null)
        {
            return existingRate;
        }

        var newRate = new ExchangeRate
        {
            TripId = tripId,
            CurrencyCodeKey = code,
            ExchangeRateValue = rate
        };

        return await _exchangeRateRepository.AddAsync(newRate);
    }
}
