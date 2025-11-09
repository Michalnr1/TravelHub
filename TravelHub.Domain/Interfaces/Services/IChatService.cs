using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravelHub.Domain.Entities;
using TravelHub.Domain.DTOs;

namespace TravelHub.Domain.Interfaces.Services
{
    /// <summary>
    /// Service contract for chat-related operations working with domain entities.
    /// </summary>
    public interface IChatService
    {
        /// <summary>
        /// Returns domain ChatMessage entities for the given trip.
        /// Person navigation property should be included by repository.
        /// </summary>
        Task<IEnumerable<ChatMessage>> GetMessagesForTripAsync(int tripId);

        /// <summary>
        /// Creates a new ChatMessage entity for the trip and returns the saved entity (with Person navigation populated).
        /// </summary>
        Task<ChatMessage> CreateMessageAsync(int tripId, ChatMessageCreateDto dto, string? currentPersonId = null);

        /// <summary>
        /// Deletes a chat message (authorization handled by service).
        /// </summary>
        Task DeleteMessageAsync(int messageId, string? currentPersonId = null);
    }
}
