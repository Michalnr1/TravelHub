using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Repositories;

public interface IPostRepository : IGenericRepository<Post>
{
    // Metody specyficzne dla Posta:

    // Pobiera post z pełnymi szczegółami
    Task<Post?> GetByIdWithDetailsAsync(int id);

    // Pobiera wszystkie posty napisane przez danego autora
    Task<IReadOnlyList<Post>> GetPostsByAuthorIdAsync(string authorId);

    // Pobiera ostatnie N postów
    Task<IReadOnlyList<Post>> GetRecentPostsAsync(int count);
}
