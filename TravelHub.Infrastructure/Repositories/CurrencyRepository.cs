using Microsoft.EntityFrameworkCore;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;

namespace TravelHub.Infrastructure.Repositories;

public class CurrencyRepository : GenericRepository<ExchangeRate>, ICurrencyRepository
{
    public CurrencyRepository(ApplicationDbContext context) : base(context)
    {
    }

    // Specyficzne implementacje metod z ICurrencyRepository, jeśli istnieją
}
