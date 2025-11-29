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
        return await _context.Posts
            .Include(p => p.Author)
            .Include(p => p.Blog)
                .ThenInclude(b => b!.Trip)
            .Include(p => p.Day)
            .Include(p => p.Comments)
                .ThenInclude(c => c.Author)
            .Include(p => p.Comments)
                .ThenInclude(c => c.Photos)
            .Include(p => p.Photos)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IReadOnlyList<Post>> GetByBlogIdAsync(int blogId)
    {
        return await _context.Posts
            .Include(p => p.Author)
            .Include(p => p.Photos)
            .Where(p => p.BlogId == blogId)
            .OrderByDescending(p => p.CreationDate)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Post>> GetPostsByAuthorIdAsync(string authorId)
    {
        return await _context.Posts
            .Include(p => p.Blog)
            .Where(p => p.AuthorId == authorId)
            .OrderByDescending(p => p.CreationDate)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Post>> GetRecentPostsAsync(int count)
    {
        return await _context.Posts
            .Include(p => p.Author)
            .OrderByDescending(p => p.CreationDate)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Post>> GetByDayIdAsync(int dayId)
    {
        return await _context.Posts
            .Include(p => p.Author)
            .Include(p => p.Photos)
            .Where(p => p.DayId == dayId)
            .OrderByDescending(p => p.CreationDate)
            .ToListAsync();
    }

    public async Task<int?> GetTripIdByPostIdAsync(int postId)
    {
        return await _context.Posts
            .Where(p => p.Id == postId)
            .Select(p => p.Blog!.TripId)
            .FirstOrDefaultAsync();
    }

    public async Task<int?> GetBlogIdByPostIdAsync(int postId)
    {
        return await _context.Posts
            .Where(p => p.Id == postId)
            .Select(p => (int?)p.BlogId)
            .FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<Post>> GetScheduledPostsByBlogIdAsync(int blogId)
    {
        return await _context.Posts
            .Where(p => p.BlogId == blogId && p.IsScheduled && p.ScheduledFor.HasValue)
            .Include(p => p.Day)
            .Include(p => p.Author)
            .Include(p => p.Photos)
            .OrderBy(p => p.ScheduledFor) // Sortuj od najbliższych w czasie
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Post>> GetPublishedPostsByBlogIdAsync(int blogId)
    {
        return await _context.Posts
            .Where(p => p.BlogId == blogId && !p.IsScheduled)
            .Include(p => p.Day)
            .Include(p => p.Author)
            .Include(p => p.Photos)
            .OrderByDescending(p => p.PublishedDate ?? p.CreationDate) // Użyj PublishedDate lub CreationDate jako fallback
            .ToListAsync();
    }
}
