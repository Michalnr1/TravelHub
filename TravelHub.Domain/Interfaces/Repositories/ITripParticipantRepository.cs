using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Repositories;

public interface ITripParticipantRepository : IGenericRepository<TripParticipant>
{
    Task<IEnumerable<TripParticipant>> GetByTripIdAsync(int tripId);
    Task<IEnumerable<TripParticipant>> GetByPersonIdAsync(string personId);
    Task<TripParticipant?> GetByTripAndPersonAsync(int tripId, string personId);
    Task<bool> ExistsAsync(int tripId, string personId);
    Task<int> GetAcceptedParticipantsCountAsync(int tripId);
    Task<IEnumerable<TripParticipant>> GetPendingInvitationsAsync(string personId);
    Task<IEnumerable<TripParticipant>> GetUserParticipatingTripsAsync(string userId);
    Task<bool> HasAcceptedParticipantAsync(int tripId, string userId);
    Task<bool> IsUserTripOwnerAsync(int tripId, string userId);
}
