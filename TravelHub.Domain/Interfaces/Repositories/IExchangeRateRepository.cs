using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Repositories;

public interface IExchangeRateRepository : IGenericRepository<ExchangeRate>
{
    // Dodaj metody specyficzne dla Currency
    Task<IReadOnlyList<ExchangeRate>> GetTripExchangeRatesAsync(int tripId);

    Task<ExchangeRate?> GetExistingRateAsync(int tripId, CurrencyCode code, decimal rate);
}
