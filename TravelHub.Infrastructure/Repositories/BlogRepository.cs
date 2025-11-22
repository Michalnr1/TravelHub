using Microsoft.EntityFrameworkCore;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;

namespace TravelHub.Infrastructure.Repositories;

public class BlogRepository : GenericRepository<Blog>, IBlogRepository
{
    public BlogRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Blog?> GetByTripIdAsync(int tripId)
    {
        return await _context.Blogs
            .Include(b => b.Trip)
            .Include(b => b.Posts)
                .ThenInclude(p => p.Author)
            .Include(b => b.Posts)
                .ThenInclude(p => p.Photos)
            .Include(b => b.Posts)
                .ThenInclude(p => p.Comments)
            .FirstOrDefaultAsync(b => b.TripId == tripId);
    }

    public async Task<Blog?> GetWithPostsAsync(int id)
    {
        return await _context.Blogs
            .Include(b => b.Trip)
            .Include(b => b.Posts)
                .ThenInclude(p => p.Author)
            .Include(b => b.Posts)
                .ThenInclude(p => p.Comments)
                    .ThenInclude(c => c.Author)
            .Include(b => b.Posts)
                .ThenInclude(p => p.Photos)
            .Include(b => b.Posts)
                .ThenInclude(p => p.Comments)
                    .ThenInclude(c => c.Photos)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<IReadOnlyList<Blog>> GetByOwnerIdAsync(string ownerId)
    {
        return await _context.Blogs
            .Include(b => b.Trip)
            .Where(b => b.OwnerId == ownerId)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Blog>> GetPublicBlogsWithDetailsAsync()
    {
        return await _context.Blogs
            .Include(b => b.Trip)
            .Include(b => b.Posts)
                .ThenInclude(p => p.Comments)
            .Include(b => b.Posts)
                .ThenInclude(p => p.Photos)
            .Where(b => !b.IsPrivate)
            .AsNoTracking()
            .ToListAsync();
    }
}
