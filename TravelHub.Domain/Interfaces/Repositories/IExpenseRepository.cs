using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Repositories;

public interface IExpenseRepository : IGenericRepository<Expense>
{
    // Dodaj metody specyficzne dla Expense
    Task<Expense?> GetByIdWithParticipantsAsync(int expenseId);
    Task<IReadOnlyList<Expense>> GetExpensesByUserIdAsync(string userId);
    Task<IEnumerable<Expense>> GetByTripIdWithParticipantsAsync(int tripId);
    Task<Expense?> GetExpenseByAccommodationIdAsync(int accommodationId);
    Task<Expense?> GetExpenseForSpotAsync(int spotId);
    Task<Expense?> GetExpenseForTransportAsync(int transportId);
}
