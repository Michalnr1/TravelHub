using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Services;

public interface ITripParticipantService : IGenericService<TripParticipant>
{
    Task<IEnumerable<TripParticipant>> GetTripParticipantsAsync(int tripId);
    Task<IEnumerable<Person>> GetFriendsAvailableForTripAsync(int tripId, string ownerId);
    Task<TripParticipant> AddParticipantAsync(int tripId, string personId);
    Task<bool> RemoveParticipantAsync(int tripId, string personId);
    Task<bool> UpdateParticipantStatusAsync(int participantId, TripParticipantStatus status);
    Task<bool> IsUserParticipantAsync(int tripId, string userId);
    Task<bool> UserHasAccessToTripAsync(int tripId, string userId);
    Task<IEnumerable<TripParticipant>> GetUserParticipatingTripsAsync(string userId);
    Task<IEnumerable<TripParticipant>> GetPendingInvitationsAsync(string userId);
    Task<int> GetParticipantCountAsync(int tripId);
    Task<TripParticipant> AddOwnerAsync(int tripId, string personId);
}
