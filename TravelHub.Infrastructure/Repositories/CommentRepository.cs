using Microsoft.EntityFrameworkCore;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;

namespace TravelHub.Infrastructure.Repositories;

public class CommentRepository : GenericRepository<Comment>, ICommentRepository
{
    public CommentRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Comment>> GetCommentsByPostIdAsync(int postId)
    {
        return await _context.Comments
            .Include(c => c.Author)
            .Include(c => c.Photos)
            .Where(c => c.PostId == postId)
            .OrderBy(c => c.CreationDate)
            .ToListAsync();
    }

    public async Task<Comment?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.Comments
            .Where(c => c.Id == id)
            .Include(c => c.Author)
            .Include(c => c.Photos)
            .FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<Comment>> GetCommentsByAuthorIdAsync(string authorId)
    {
        return await _context.Comments
            .Include(c => c.Post)
            .Where(c => c.AuthorId == authorId)
            .OrderByDescending(c => c.CreationDate)
            .ToListAsync();
    }
}
