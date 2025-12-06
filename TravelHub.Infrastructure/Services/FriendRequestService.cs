using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TravelHub.Domain.DTOs;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;

namespace TravelHub.Infrastructure.Services;

public class FriendRequestService : IFriendRequestService
{
    private readonly IFriendRequestRepository _friendRequestRepository;
    private readonly IPersonFriendsRepository _personFriendsRepository;
    private readonly UserManager<Person> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<FriendRequestService> _logger;

    public FriendRequestService(
        IFriendRequestRepository friendRequestRepository,
        IPersonFriendsRepository personFriendsRepository,
        UserManager<Person> userManager,
        IEmailSender emailSender,
        IHttpContextAccessor httpContextAccessor,
        ILogger<FriendRequestService> logger)
    {
        _friendRequestRepository = friendRequestRepository;
        _personFriendsRepository = personFriendsRepository;
        _userManager = userManager;
        _emailSender = emailSender;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<FriendRequestResult> SendFriendRequestAsync(string requesterId, string addresseeIdentifier, string? message = null)
    {
        try
        {
            var addressee = await _userManager.Users
                .FirstOrDefaultAsync(u => u.UserName == addresseeIdentifier || u.Email == addresseeIdentifier || u.Id == addresseeIdentifier);

            if (addressee == null)
            {
                return new FriendRequestResult { Success = false, Message = "User not found." };
            }

            if (requesterId == addressee.Id)
            {
                return new FriendRequestResult { Success = false, Message = "You cannot send friend request to yourself." };
            }

            if (await _personFriendsRepository.FriendshipExistsAsync(requesterId, addressee.Id))
            {
                return new FriendRequestResult { Success = false, Message = "You are already friends with this user." };
            }

            if (await _friendRequestRepository.HasPendingRequestAsync(requesterId, addressee.Id))
            {
                return new FriendRequestResult { Success = false, Message = "Friend request already exists." };
            }

            var friendRequest = new FriendRequest
            {
                RequesterId = requesterId,
                AddresseeId = addressee.Id,
                Status = FriendRequestStatus.Pending,
                Message = message,
                RequestedAt = DateTimeOffset.UtcNow
            };

            await _friendRequestRepository.AddAsync(friendRequest);
            await SendFriendRequestEmail(friendRequest, addressee);

            _logger.LogInformation("Friend request sent from {RequesterId} to {AddresseeId}", requesterId, addressee.Id);

            return new FriendRequestResult { Success = true, Message = "Friend request sent successfully.", FriendRequest = friendRequest };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending friend request from {RequesterId} to {AddresseeIdentifier}", requesterId, addresseeIdentifier);
            return new FriendRequestResult { Success = false, Message = "An error occurred while sending friend request." };
        }
    }

    public async Task<FriendRequestResult> AcceptFriendRequestAsync(int friendRequestId, string userId)
    {
        try
        {
            var friendRequest = await _friendRequestRepository.GetByIdAsync(friendRequestId);

            if (friendRequest == null || friendRequest.AddresseeId != userId)
            {
                return new FriendRequestResult { Success = false, Message = "Friend request not found." };
            }

            if (friendRequest.Status != FriendRequestStatus.Pending)
            {
                return new FriendRequestResult { Success = false, Message = "Friend request is not pending." };
            }

            friendRequest.Status = FriendRequestStatus.Accepted;
            friendRequest.RespondedAt = DateTimeOffset.UtcNow;
            await _friendRequestRepository.UpdateAsync(friendRequest);

            var friendship1 = new PersonFriends
            {
                UserId = friendRequest.RequesterId,
                FriendId = friendRequest.AddresseeId,
                CreatedAt = DateTimeOffset.UtcNow
            };

            var friendship2 = new PersonFriends
            {
                UserId = friendRequest.AddresseeId,
                FriendId = friendRequest.RequesterId,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _personFriendsRepository.AddAsync(friendship1);
            await _personFriendsRepository.AddAsync(friendship2);

            await SendFriendRequestAcceptedEmail(friendRequest);

            _logger.LogInformation("Friend request {FriendRequestId} accepted by {UserId}", friendRequestId, userId);

            return new FriendRequestResult { Success = true, Message = "Friend request accepted.", FriendRequest = friendRequest };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting friend request {FriendRequestId} by {UserId}", friendRequestId, userId);
            return new FriendRequestResult { Success = false, Message = "An error occurred while accepting friend request." };
        }
    }

    public async Task<FriendRequestResult> DeclineFriendRequestAsync(int friendRequestId, string userId)
    {
        try
        {
            var friendRequest = await _friendRequestRepository.GetByIdAsync(friendRequestId);

            if (friendRequest == null || friendRequest.AddresseeId != userId)
            {
                return new FriendRequestResult { Success = false, Message = "Friend request not found." };
            }

            friendRequest.Status = FriendRequestStatus.Declined;
            friendRequest.RespondedAt = DateTimeOffset.UtcNow;
            await _friendRequestRepository.UpdateAsync(friendRequest);

            _logger.LogInformation("Friend request {FriendRequestId} declined by {UserId}", friendRequestId, userId);

            return new FriendRequestResult { Success = true, Message = "Friend request declined.", FriendRequest = friendRequest };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error declining friend request {FriendRequestId} by {UserId}", friendRequestId, userId);
            return new FriendRequestResult { Success = false, Message = "An error occurred while declining friend request." };
        }
    }

    public async Task<FriendRequestResult> CancelFriendRequestAsync(int friendRequestId, string userId)
    {
        try
        {
            var friendRequest = await _friendRequestRepository.GetByIdAsync(friendRequestId);

            if (friendRequest == null || friendRequest.RequesterId != userId)
            {
                return new FriendRequestResult { Success = false, Message = "Friend request not found." };
            }

            await _friendRequestRepository.DeleteAsync(friendRequest);

            _logger.LogInformation("Friend request {FriendRequestId} cancelled by {UserId}", friendRequestId, userId);

            return new FriendRequestResult { Success = true, Message = "Friend request cancelled." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling friend request {FriendRequestId} by {UserId}", friendRequestId, userId);
            return new FriendRequestResult { Success = false, Message = "An error occurred while cancelling friend request." };
        }
    }

    public async Task<IReadOnlyList<FriendRequest>> GetPendingRequestsAsync(string userId)
    {
        return await _friendRequestRepository.GetPendingRequestsAsync(userId);
    }

    public async Task<IReadOnlyList<FriendRequest>> GetSentRequestsAsync(string userId)
    {
        return await _friendRequestRepository.GetSentRequestsAsync(userId);
    }

    public async Task<IReadOnlyList<Person>> GetPublicUsersAsync(string currentUserId)
    {
        return await _userManager.Users
            .Where(u => u.Id != currentUserId && !u.IsPrivate)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToListAsync();
    }

    public async Task<bool> HasPendingRequestAsync(string user1Id, string user2Id)
    {
        return await _friendRequestRepository.HasPendingRequestAsync(user1Id, user2Id);
    }

    private async Task SendFriendRequestEmail(FriendRequest friendRequest, Person addressee)
    {
        try
        {
            var emailSubject = "New Friend Request on TravelHub";
            var emailMessage = $@"
                <h3>Hello {addressee.FirstName}!</h3>
                <p>You have received a friend request from {friendRequest.Requester.FirstName} {friendRequest.Requester.LastName}.</p>
                {(string.IsNullOrEmpty(friendRequest.Message) ? "" : $"<p><strong>Message:</strong> {friendRequest.Message}</p>")}
                <p>Please log in to your account to accept or decline the request.</p>
                <a href='{GetAppBaseUrl()}/Friends/Pending' style='background-color: #4CAF50; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>View Request</a>
                <br/><br/>
                <p>Best regards,<br/>TravelHub Team</p>";

            await _emailSender.SendEmailAsync(addressee.Email!, emailSubject, emailMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending friend request email to {Email}", addressee.Email);
        }
    }

    private async Task SendFriendRequestAcceptedEmail(FriendRequest friendRequest)
    {
        try
        {
            var requester = await _userManager.FindByIdAsync(friendRequest.RequesterId);
            if (requester?.Email == null) return;

            var emailSubject = "Friend Request Accepted on TravelHub";
            var emailMessage = $@"
                <h3>Hello {requester.FirstName}!</h3>
                <p>Your friend request to {friendRequest.Addressee.FirstName} {friendRequest.Addressee.LastName} has been accepted.</p>
                <p>You are now friends on TravelHub!</p>
                <a href='{GetAppBaseUrl()}/Friends' style='background-color: #4CAF50; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>View Friends</a>
                <br/><br/>
                <p>Best regards,<br/>TravelHub Team</p>";

            await _emailSender.SendEmailAsync(requester.Email, emailSubject, emailMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending friend request accepted email to {RequesterId}", friendRequest.RequesterId);
        }
    }

    private string GetAppBaseUrl()
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        if (request != null)
        {
            return $"{request.Scheme}://{request.Host}";
        }

        return "https://localhost:7181";
    }
}