using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Repositories;

public interface IPhotoRepository : IGenericRepository<Photo>
{
    Task<IReadOnlyList<Photo>> GetPhotosBySpotIdAsync(int spotId);

    Task<IReadOnlyList<Photo>> GetPhotosByPostIdAsync(int postId);
}
