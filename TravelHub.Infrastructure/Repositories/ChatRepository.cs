using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;

namespace TravelHub.Infrastructure.Repositories
{
    /// <summary>
    /// Chat repository that reuses GenericRepository for CRUD
    /// and adds chat-specific queries that require EF includes.
    /// </summary>
    public class ChatRepository : GenericRepository<ChatMessage>, IChatRepository
    {
        public ChatRepository(ApplicationDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Get all chat messages for a trip including the Person navigation property.
        /// Ordered by Id.
        /// </summary>
        public async Task<IEnumerable<ChatMessage>> GetByTripIdAsync(int tripId)
        {
            return await _context.Set<ChatMessage>()
                                 .Include(c => c.Person)
                                 .Where(c => c.TripId == tripId)
                                 .OrderBy(c => c.Id)
                                 .ToListAsync();
        }

        /// <summary>
        /// Get a single chat message by id including the Person navigation property.
        /// </summary>
        public async Task<ChatMessage?> GetByIdWithPersonAsync(int id)
        {
            return await _context.Set<ChatMessage>()
                                 .Include(c => c.Person)
                                 .FirstOrDefaultAsync(c => c.Id == id);
        }
    }
}
