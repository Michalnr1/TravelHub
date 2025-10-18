using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Repositories;

public interface IPhotoRepository : IGenericRepository<Photo>
{
    // Metody specyficzne dla Photo:

    // Pobiera wszystkie zdjęcia związane z danym Spotem.
    Task<IReadOnlyList<Photo>> GetPhotosBySpotIdAsync(int spotId);

    // Pobiera wszystkie zdjęcia przypisane do konkretnego Posta.
    Task<IReadOnlyList<Photo>> GetPhotosByPostIdAsync(int postId);
}
