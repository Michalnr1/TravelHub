using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;

namespace TravelHub.Infrastructure.Services;

// Teraz dziedziczy z serwisu generycznego
public class ExpenseService : GenericService<Expense>, IExpenseService
{
    private readonly IExpenseRepository _expenseRepository; // Specyficzne repozytorium do metod customowych

    public ExpenseService(IExpenseRepository expenseRepository)
        : base(expenseRepository) // Przekazujemy repozytorium do serwisu generycznego
    {
        _expenseRepository = expenseRepository;
    }

    // Metody specyficzne dla Expense
    public async Task<IReadOnlyList<Expense>> GetUserExpensesAsync(string userId)
    {
        return await _expenseRepository.GetExpensesByUserIdAsync(userId);
    }

    public async Task<IEnumerable<Expense>> GetByTripIdWithParticipantsAsync(int tripId)
    {
        return await _expenseRepository.GetByTripIdWithParticipantsAsync(tripId);
    }
}