using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Repositories;

public interface ICommentRepository : IGenericRepository<Comment>
{
    // Metody specyficzne dla Komentarza:

    // Pobiera wszystkie komentarze dla danego Posta, posortowane chronologicznie
    Task<IReadOnlyList<Comment>> GetCommentsByPostIdAsync(int postId);

    // Pobiera komentarz wraz z powiązanymi danymi
    Task<Comment?> GetByIdWithDetailsAsync(int id);

    // Pobiera wszystkie komentarze napisane przez danego użytkownika
    Task<IReadOnlyList<Comment>> GetCommentsByAuthorIdAsync(string authorId);
}
