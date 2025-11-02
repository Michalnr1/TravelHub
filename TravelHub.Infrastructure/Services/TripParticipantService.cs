using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;
using static System.Net.Mime.MediaTypeNames;

namespace TravelHub.Infrastructure.Services;

public class TripParticipantService : GenericService<TripParticipant>, ITripParticipantService
{
    private readonly ITripParticipantRepository _tripParticipantRepository;
    private readonly IGenericRepository<PersonFriends> _personFriendsRepository;
    private readonly IGenericRepository<Trip> _tripRepository;
    private readonly UserManager<Person> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<TripParticipantService> _logger;

    public TripParticipantService(
        ITripParticipantRepository repository,
        IGenericRepository<PersonFriends> personFriendsRepository,
        IGenericRepository<Trip> tripRepository,
        UserManager<Person> userManager,
        IEmailSender emailSender,
        ILogger<TripParticipantService> logger) : base(repository)
    {
        _tripParticipantRepository = repository;
        _personFriendsRepository = personFriendsRepository;
        _tripRepository = tripRepository;
        _userManager = userManager;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<IEnumerable<TripParticipant>> GetTripParticipantsAsync(int tripId)
    {
        return await _tripParticipantRepository.GetByTripIdAsync(tripId);
    }

    public async Task<IEnumerable<Person>> GetFriendsAvailableForTripAsync(int tripId, string ownerId)
    {
        try
        {
            // Pobierz istniejących uczestników wycieczki
            var existingParticipants = await _tripParticipantRepository.GetByTripIdAsync(tripId);
            var existingParticipantIds = existingParticipants.Select(p => p.PersonId).ToList();

            // Pobierz wszystkich znajomych właściciela
            var allFriends = await _personFriendsRepository.GetAllAsync();

            var friendIds = allFriends
                .Where(pf => (pf.UserId == ownerId && pf.FriendId != ownerId) ||
                            (pf.FriendId == ownerId && pf.UserId != ownerId))
                .Select(pf => pf.UserId == ownerId ? pf.FriendId : pf.UserId)
                .Where(id => !existingParticipantIds.Contains(id))
                .Distinct()
                .ToList();

            if (!friendIds.Any())
                return new List<Person>();

            // Pobierz wszystkich znajomych w jednym zapytaniu
            var friends = new List<Person>();
            foreach (var friendId in friendIds)
            {
                var friend = await _userManager.FindByIdAsync(friendId);
                if (friend != null && !friend.IsPrivate)
                {
                    friends.Add(friend);
                }
            }

            return friends.OrderBy(f => f.FirstName).ThenBy(f => f.LastName).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available friends for trip {TripId} and owner {OwnerId}", tripId, ownerId);
            return new List<Person>();
        }
    }

    public async Task<TripParticipant> AddParticipantAsync(int tripId, string personId)
    {
        var tripParticipant = new TripParticipant
        {
            TripId = tripId,
            PersonId = personId,
            Status = TripParticipantStatus.Pending,
            JoinedAt = DateTime.UtcNow
        };

        var result = await _tripParticipantRepository.AddAsync(tripParticipant);

        // Wyślij email z zaproszeniem
        await SendTripInvitationEmail(result);

        return result;
    }

    public async Task<bool> RemoveParticipantAsync(int tripId, string personId)
    {
        var participant = await _tripParticipantRepository.GetByTripAndPersonAsync(tripId, personId);
        if (participant == null)
            return false;

        await _tripParticipantRepository.DeleteAsync(participant);
        return true;
    }

    public async Task<bool> UpdateParticipantStatusAsync(int participantId, TripParticipantStatus status)
    {
        var participant = await _tripParticipantRepository.GetByIdAsync(participantId);
        if (participant == null)
            return false;

        participant.Status = status;
        await _tripParticipantRepository.UpdateAsync(participant);

        if (status == TripParticipantStatus.Accepted)
        {
            await SendInvitationAcceptedEmail(participant);
        }

        return true;
    }

    public async Task<bool> IsUserParticipantAsync(int tripId, string userId)
    {
        return await _tripParticipantRepository.ExistsAsync(tripId, userId);
    }

    public async Task<bool> UserHasAccessToTripAsync(int tripId, string userId)
    {
        // Sprawdź właściciela
        var isOwner = await _tripParticipantRepository.IsUserTripOwnerAsync(tripId, userId);
        if (isOwner)
            return true;

        // Sprawdź uczestnictwo
        return await _tripParticipantRepository.HasAcceptedParticipantAsync(tripId, userId);
    }

    public async Task<IEnumerable<TripParticipant>> GetUserParticipatingTripsAsync(string userId)
    {
        return await _tripParticipantRepository.GetByPersonIdAsync(userId);
    }

    public async Task<IEnumerable<TripParticipant>> GetPendingInvitationsAsync(string userId)
    {
        return await _tripParticipantRepository.GetPendingInvitationsAsync(userId);
    }

    public async Task<int> GetParticipantCountAsync(int tripId)
    {
        return await _tripParticipantRepository.GetAcceptedParticipantsCountAsync(tripId);
    }

    private async Task SendTripInvitationEmail(TripParticipant participant)
    {
        try
        {
            var trip = await _tripRepository.GetByIdAsync(participant.TripId) as Trip;
            var invitedPerson = await _userManager.FindByIdAsync(participant.PersonId);
            var owner = await _userManager.FindByIdAsync(trip?.PersonId!);

            if (trip == null || invitedPerson == null || owner == null || string.IsNullOrEmpty(invitedPerson.Email))
                return;

            var subject = $"You've been invited to trip: {trip.Name}";

            var htmlMessage = $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
                    .container { max-width: 600px; margin: 0 auto; padding: 20px; }
                    .header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }
                    .content { background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }
                    .button { display: inline-block; padding: 12px 30px; background: #667eea; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }
                    .footer { text-align: center; margin-top: 20px; font-size: 12px; color: #666; }
                </style>
            </head>
            <body>
                <div class="container">
                    <div class="header">
                        <h1>🎒 TravelHub Trip Invitation</h1>
                    </div>
                    <div class="content">
                        <h2>Hello {{invitedPerson.FirstName}}!</h2>
                        <p>You have been invited by <strong>{{owner.FirstName}} {{owner.LastName}}</strong> to join the trip:</p>
                        <h3>"{{trip.Name}}"</h3>
                        <p><strong>Dates:</strong> {{trip.StartDate:MMM dd, yyyy}} - {{trip.EndDate:MMM dd, yyyy}}</p>
                        <p>Log in to your TravelHub account to view the trip details and accept the invitation.</p>
                        <div style="text-align: center;">
                            <a href="{{GetAppBaseUrl()}}/Trips/MyTrips" class="button">View Trip in TravelHub</a>
                        </div>
                        <p>If you believe this invitation was sent in error, please ignore this email.</p>
                    </div>
                    <div class="footer">
                        <p>This is an automated message from TravelHub. Please do not reply to this email.</p>
                    </div>
                </div>
            </body>
            </html>
            """;

            await _emailSender.SendEmailAsync(invitedPerson.Email, subject, htmlMessage);
            _logger.LogInformation("Trip invitation email sent to {Email}", invitedPerson.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending trip invitation email for participant {ParticipantId}", participant.Id);
        }
    }

    private async Task SendInvitationAcceptedEmail(TripParticipant participant)
    {
        try
        {
            var trip = await _tripRepository.GetByIdAsync(participant.TripId) as Trip;
            var acceptedPerson = await _userManager.FindByIdAsync(participant.PersonId);
            var owner = await _userManager.FindByIdAsync(trip?.PersonId!);

            if (trip == null || acceptedPerson == null || owner == null || string.IsNullOrEmpty(owner.Email))
                return;

            var subject = $"Trip invitation accepted - {trip.Name}";

            var htmlMessage = $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
                    .container { max-width: 600px; margin: 0 auto; padding: 20px; }
                    .header { background: linear-gradient(135deg, #4CAF50 0%, #45a049 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }
                    .content { background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }
                    .footer { text-align: center; margin-top: 20px; font-size: 12px; color: #666; }
                </style>
            </head>
            <body>
                <div class="container">
                    <div class="header">
                        <h1>✅ Invitation Accepted</h1>
                    </div>
                    <div class="content">
                        <h2>Great news, {{owner.FirstName}}!</h2>
                        <p><strong>{{acceptedPerson.FirstName}} {{acceptedPerson.LastName}}</strong> has accepted your invitation to join the trip:</p>
                        <h3>"{{trip.Name}}"</h3>
                        <p>They can now view all the trip details and participate in planning.</p>
                        <br/>
                        <p>Happy travels!<br/>TravelHub Team</p>
                    </div>
                    <div class="footer">
                        <p>This is an automated message from TravelHub. Please do not reply to this email.</p>
                    </div>
                </div>
            </body>
            </html>
            """;

            await _emailSender.SendEmailAsync(owner.Email, subject, htmlMessage);
            _logger.LogInformation("Invitation accepted email sent to trip owner {OwnerEmail}", owner.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending invitation accepted email for participant {ParticipantId}", participant.Id);
        }
    }

    private string GetAppBaseUrl()
    {
        // W prawdziwej aplikacji pobierz to z konfiguracji
        return "https://localhost:7181";
    }
}