using Microsoft.EntityFrameworkCore;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;

namespace TravelHub.Infrastructure.Repositories;

public class ExchangeRateRepository : GenericRepository<ExchangeRate>, IExchangeRateRepository
{
    public ExchangeRateRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<ExchangeRate>> GetTripExchangeRatesAsync(int tripId)
    {
        return await _context.ExchangeRates
            .Where(er => er.TripId == tripId)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<ExchangeRate?> GetExistingRateAsync(int tripId, CurrencyCode code, decimal rate)
    {
        return await _context.ExchangeRates
            .FirstOrDefaultAsync(er => er.TripId == tripId
                                    && er.CurrencyCodeKey == code
                                    && er.ExchangeRateValue == rate);
    }
}
