using Microsoft.EntityFrameworkCore;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;

namespace TravelHub.Infrastructure.Repositories;

public class PostRepository : GenericRepository<Post>, IPostRepository
{
    public PostRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Post?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.Set<Post>()
            .Where(p => p.Id == id)
            .Include(p => p.Author)
            .Include(p => p.Comments)
            .Include(p => p.Photos)
            .FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<Post>> GetPostsByAuthorIdAsync(string authorId)
    {
        return await _context.Set<Post>()
            .Where(p => p.AuthorId == authorId)
            .Include(p => p.Author)
            .OrderByDescending(p => p.CreationDate)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Post>> GetRecentPostsAsync(int count)
    {
        return await _context.Set<Post>()
            .Include(p => p.Author)
            .OrderByDescending(p => p.CreationDate)
            .Take(count)
            .ToListAsync();
    }
}
