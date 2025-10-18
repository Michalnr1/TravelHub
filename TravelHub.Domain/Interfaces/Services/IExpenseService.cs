using System.Collections.Generic;
using System.Threading.Tasks;
using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Services;

public interface IExpenseService : IGenericService<Expense>
{
    // Metody specyficzne dla Expense
    Task<IReadOnlyList<Expense>> GetUserExpensesAsync(string userId);
}
