using Microsoft.EntityFrameworkCore;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;

namespace TravelHub.Infrastructure.Repositories;

public class ExpenseRepository : GenericRepository<Expense>, IExpenseRepository
{
    public ExpenseRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Expense?> GetByIdWithParticipantsAsync(int expenseId)
    {
        return await _context.Expenses
            .Include(e => e.Participants)
                .ThenInclude(ep => ep.Person)
            .Include(e => e.ExchangeRate)
            .Include(e => e.Trip)
            .Include(e => e.PaidBy)
            .FirstOrDefaultAsync(e => e.Id == expenseId);
    }

    public async Task<IReadOnlyList<Expense>> GetExpensesByUserIdAsync(string userId)
    {
        return await _context.Set<Expense>()
            .Where(e => e.PaidById == userId)
            // Opcjonalnie dołącz powiązane encje (np. Currency, Category)
            // .Include(e => e.Currency) 
            // .Include(e => e.Category)
            .ToListAsync();
    }

    public async Task<IEnumerable<Expense>> GetByTripIdWithParticipantsAsync(int tripId)
    {
        return await _context.Expenses
            .Include(e => e.Category)
            .Include(e => e.ExchangeRate)
            .Include(e => e.PaidBy)
            .Include(e => e.Participants!)
                .ThenInclude(ep => ep.Person)
            .Where(e => e.TripId == tripId)
            .ToListAsync();
    }

    //public async Task<IEnumerable<Expense>> GetByTripIdWithExchangeRatesAsync(int tripId)
    //{
    //    return await _context.Expenses
    //        .Include(e => e.ExchangeRate)
    //        .Include(e => e.PaidBy)
    //        .Include(e => e.Category)
    //        .Include(e => e.Participants!)
    //            .ThenInclude(ep => ep.Person)
    //        .Where(e => e.TripId == tripId)
    //        .ToListAsync();
    //}
}
