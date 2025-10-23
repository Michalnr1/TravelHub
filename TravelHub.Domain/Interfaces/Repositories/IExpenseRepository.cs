using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Repositories;

public interface IExpenseRepository : IGenericRepository<Expense>
{
    // Dodaj metody specyficzne dla Expense
    Task<IReadOnlyList<Expense>> GetExpensesByUserIdAsync(string userId);
    Task<IEnumerable<Expense>> GetByTripIdWithParticipantsAsync(int tripId);
}
