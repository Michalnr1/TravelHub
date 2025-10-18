using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Repositories;

public interface ICategoryRepository : IGenericRepository<Category>
{
    // Metody specyficzne dla Category:

    // Pobiera kategorię wraz ze wszystkimi powiązanymi aktywnościami i wydatkami.
    Task<Category?> GetByIdWithRelatedDataAsync(int id);

    // Sprawdza, czy kategoria o danej nazwie już istnieje
    Task<bool> ExistsByNameAsync(string name);
}
