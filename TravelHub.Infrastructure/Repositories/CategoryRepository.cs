using Microsoft.EntityFrameworkCore;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;

namespace TravelHub.Infrastructure.Repositories;

public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
{
    public CategoryRepository(ApplicationDbContext context, ITripParticipantRepository tripParticipantRepository) : base(context)
    {
    }

    public async Task<ICollection<Category>> GetAllCategoriesByUserAsync(string userId)
    {
        return await _context.Set<Category>()
            .Where(c => c.PersonId == userId)
            .ToListAsync();
    }

    public async Task<Category?> GetByIdWithRelatedDataAsync(int id)
    {
        return await _context.Set<Category>()
            .Where(c => c.Id == id)
            .Include(c => c.Activities)
            .Include(c => c.Expenses)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        // Porównanie ignorujące wielkość liter
        return await _context.Set<Category>()
            .AnyAsync(c => c.Name.ToLower() == name.ToLower());
    }

    public async Task<bool> IsInUseAsync(int categoryId)
    {
        // Sprawdź, czy którakolwiek Activity (w tym Spot) odwołuje się do tej kategorii
        var usedInActivities = await _context.Set<Activity>()
            .AnyAsync(a => a.CategoryId == categoryId);

        if (usedInActivities) return true;

        // Sprawdź wydatki
        var usedInExpenses = await _context.Set<Expense>()
            .AnyAsync(e => e.CategoryId == categoryId);

        return usedInExpenses;
    }

    public async Task<ICollection<Category>> GetAllCategoriesByTripAsync(int tripId)
    {
        var acceptedPersonIds = _context.Set<TripParticipant>()
        .Where(tp => tp.TripId == tripId && (tp.Status == TripParticipantStatus.Accepted || tp.Status == TripParticipantStatus.Owner))
        .Select(tp => tp.PersonId);

        var uniqueCategoryIds = await _context.Set<Category>()
            .Where(c => acceptedPersonIds.Contains(c.PersonId))
            .GroupBy(c => c.Name)
            .Select(g => g.Min(c => c.Id))
            .ToListAsync();

        var categories = await _context.Set<Category>()
            .Where(c => uniqueCategoryIds.Contains(c.Id))
            .OrderBy(c => c.Name)
            .ToListAsync();

        return categories;
    }
}
