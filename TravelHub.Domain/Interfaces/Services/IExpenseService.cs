using TravelHub.Domain.DTOs;
using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Services;

public interface IExpenseService : IGenericService<Expense>
{
    // Zmieniona sygnatura AddAsync
    Task<Expense> AddAsync(Expense entity, IEnumerable<ParticipantShareDto> participantShares);
    
    // Zmieniona sygnatura UpdateAsync
    Task UpdateAsync(Expense entity, IEnumerable<ParticipantShareDto> participantShares);

    // Metody specyficzne dla Expense
    Task<IReadOnlyList<Expense>> GetUserExpensesAsync(string userId);
    Task<IEnumerable<Expense>> GetByTripIdWithParticipantsAsync(int tripId);
    Task<TripExpensesSummaryDto> CalculateTripExpensesInTripCurrencyAsync(int tripId, CurrencyCode tripCurrency);
    Task<decimal> GetTotalExpensesInCurrencyAsync(int tripId, CurrencyCode targetCurrency);
    Task<Expense?> GetExpenseByAccommodationIdAsync(int accommodationId);
}
