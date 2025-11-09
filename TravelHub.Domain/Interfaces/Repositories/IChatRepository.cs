using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Repositories
{
    /// <summary>
    /// Repository specialized for ChatMessage entity.
    /// Extends the generic repository with chat-specific queries.
    /// </summary>
    public interface IChatRepository : IGenericRepository<ChatMessage>
    {
        /// <summary>
        /// Returns messages for a specific trip including Person navigation property.
        /// </summary>
        Task<IEnumerable<ChatMessage>> GetByTripIdAsync(int tripId);

        /// <summary>
        /// Returns a single ChatMessage with Person included.
        /// </summary>
        Task<ChatMessage?> GetByIdWithPersonAsync(int id);
    }
}
