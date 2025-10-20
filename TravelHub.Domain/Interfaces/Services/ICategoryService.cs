using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Services;

public interface ICategoryService : IGenericService<Category>
{
    Task<bool> ExistsByNameAsync(string name);

    // Sprawdza czy kategoria jest używana (Activity/Spot/Expense)
    Task<bool> IsInUseAsync(int categoryId);
}