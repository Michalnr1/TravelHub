using Microsoft.EntityFrameworkCore;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;

namespace TravelHub.Infrastructure.Repositories;

public class PhotoRepository : GenericRepository<Photo>, IPhotoRepository
{
    public PhotoRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Photo>> GetPhotosBySpotIdAsync(int spotId)
    {
        return await _context.Set<Photo>()
            .Where(p => p.SpotId == spotId)
            .Include(p => p.Spot)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Photo>> GetPhotosByPostIdAsync(int postId)
    {
        return await _context.Set<Photo>()
            .Where(p => p.PostId == postId)
            .Include(p => p.Post)
            .ToListAsync();
    }
}
