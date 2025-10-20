using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Repositories;

public interface ICategoryRepository : IGenericRepository<Category>
{
    // Pobiera kategorię wraz ze wszystkimi powiązanymi aktywnościami i wydatkami.
    Task<Category?> GetByIdWithRelatedDataAsync(int id);

    // Sprawdza, czy kategoria o danej nazwie już istnieje
    Task<bool> ExistsByNameAsync(string name);

    // Sprawdza, czy kategoria jest używana przez jakiekolwiek Activity/Spot lub Expense
    Task<bool> IsInUseAsync(int categoryId);
}