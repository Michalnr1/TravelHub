using TravelHub.Domain.Entities;
using TravelHub.Domain.DTOs;
using TravelHub.Application.DTOs;

namespace TravelHub.Domain.Interfaces.Services;

public interface ITripService : IGenericService<Trip>
{
    Task<Trip?> GetTripWithDetailsAsync(int id);
    Task<IEnumerable<Trip>> GetUserTripsAsync(string userId);
    Task<Day> AddDayToTripAsync(int tripId, Day day);
    Task<IEnumerable<Day>> GetTripDaysAsync(int tripId);
    Task<bool> UserOwnsTripAsync(int tripId, string userId);
    Task<bool> ExistsAsync(int id);
    Task<(double medianLatitude, double medianLongitude)> GetMedianCoords(int id);
    Task<IEnumerable<Trip>> GetAllWithUserAsync();
    Task<Day> CreateNextDayAsync(int tripId);
    Task<CurrencyCode> GetTripCurrencyAsync(int tripId);
    Task<IEnumerable<Person>> GetAllTripParticipantsAsync(int tripId);
    Task<Checklist> GetChecklistAsync(int tripId);
    Task AddChecklistItemAsync(int tripId, string item);
    Task ToggleChecklistItemAsync(int tripId, string item);
    Task RemoveChecklistItemAsync(int tripId, string item);
    Task ReplaceChecklistAsync(int tripId, Checklist newChecklist);
    Task RenameChecklistItemAsync(int tripId, string oldItem, string newItem);
    Task AssignParticipantToItemAsync(int tripId, string itemTitle, string? participantId);
    Task<Trip?> GetByIdWithParticipantsAsync(int id);
    Task<Blog?> GetOrCreateBlogForTripAsync(int tripId, string userId);
    Task<bool> HasBlogAsync(int tripId);
    Task<IEnumerable<PublicTripDto>> SearchPublicTripsAsync(PublicTripSearchCriteriaDto criteria);
    Task<IEnumerable<Country>> GetAvailableCountriesForPublicTripsAsync();
    int CountAllSpotsInTrip(Trip trip);
    List<string> GetUniqueCountriesFromTrip(Trip trip);
    Task MarkAllChecklistItemsAsync(int tripId, bool completed);
    Task<Trip> CloneTripAsync(int sourceTripId, string cloningUserId, CloneTripRequestDto request);
    Task<double> GetDistance(int id, double lat, double lng);
}