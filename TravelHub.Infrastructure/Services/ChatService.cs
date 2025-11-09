using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Domain.DTOs;

namespace TravelHub.Infrastructure.Services
{
    /// <summary>
    /// Chat service reusing generic service for basic CRUD and providing chat-specific logic.
    /// Returns domain ChatMessage entities.
    /// </summary>
    public class ChatService : GenericService<ChatMessage>, IChatService
    {
        private readonly IChatRepository _chatRepository;
        private readonly ITripRepository _tripRepository;
        private readonly IGenericRepository<Person>? _personRepository;

        public ChatService(
            IChatRepository chatRepository,
            ITripRepository tripRepository,
            IGenericRepository<Person>? personRepository = null)
            : base(chatRepository)
        {
            _chatRepository = chatRepository;
            _tripRepository = tripRepository;
            _personRepository = personRepository;
        }

        /// <summary>
        /// Returns chat messages (domain entities) for a trip.
        /// The Person navigation property is expected to be included by the repository.
        /// </summary>
        public async Task<IEnumerable<ChatMessage>> GetMessagesForTripAsync(int tripId)
        {
            var messages = await _chatRepository.GetByTripIdAsync(tripId);
            return messages;
        }

        /// <summary>
        /// Creates a new ChatMessage and returns the saved entity with Person included.
        /// </summary>
        public async Task<ChatMessage> CreateMessageAsync(int tripId, ChatMessageCreateDto dto, string? currentPersonId = null)
        {
            if (string.IsNullOrWhiteSpace(dto.Message))
                throw new ArgumentException("Message cannot be empty.", nameof(dto.Message));

            var trip = await _tripRepository.GetByIdAsync(tripId);
            if (trip == null)
                throw new KeyNotFoundException($"Trip with id {tripId} not found.");

            var personId = currentPersonId ?? dto.PersonId;
            if (string.IsNullOrEmpty(personId))
                throw new InvalidOperationException("PersonId is required to create a message.");

            if (_personRepository != null)
            {
                var person = await _personRepository.GetByIdAsync(personId);
                if (person == null)
                    throw new KeyNotFoundException($"Person with id {personId} not found.");
            }

            var chat = new ChatMessage
            {
                Message = dto.Message.Trim(),
                PersonId = personId,
                TripId = tripId
            };

            // Use generic AddAsync (from GenericRepository)
            var added = await _chatRepository.AddAsync(chat);

            // Reload entity with Person navigation included
            var loaded = await _chatRepository.GetByIdWithPersonAsync(added.Id) ?? added;

            return loaded;
        }

        /// <summary>
        /// Deletes the ChatMessage entity.
        /// Only the message owner is allowed.
        /// </summary>
        public async Task DeleteMessageAsync(int messageId, string? currentPersonId = null)
        {
            var message = await _chatRepository.GetByIdWithPersonAsync(messageId);
            if (message == null) return;

            if (currentPersonId != null && message.PersonId != currentPersonId)
                throw new UnauthorizedAccessException("You are not allowed to delete this message.");

            await _chatRepository.DeleteAsync(message);
        }
    }
}
