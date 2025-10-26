using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Services;

public interface IExchangeRateService : IGenericService<ExchangeRate>
{
    Task<IReadOnlyList<ExchangeRate>> GetTripExchangeRatesAsync(int tripId);

    Task<ExchangeRate> GetOrCreateExchangeRateAsync(int tripId, CurrencyCode code, decimal rate);
}
