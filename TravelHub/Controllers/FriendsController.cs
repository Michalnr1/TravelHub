using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TravelHub.Domain.DTOs;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Services;

namespace TravelHub.Web.Controllers;

[Authorize]
public class FriendsController : Controller
{
    private readonly IFriendRequestService _friendRequestService;
    private readonly IFriendshipService _friendshipService;
    private readonly UserManager<Person> _userManager;

    public FriendsController(
        IFriendRequestService friendRequestService,
        IFriendshipService friendshipService,
        UserManager<Person> userManager)
    {
        _friendRequestService = friendRequestService;
        _friendshipService = friendshipService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null) return RedirectToAction("Login", "Account");

        var friends = await _friendshipService.GetFriendsAsync(currentUser.Id);
        var friendsCount = await _friendshipService.GetFriendsCountAsync(currentUser.Id);

        ViewBag.FriendsCount = friendsCount;
        return View(friends);
    }

    public async Task<IActionResult> AddFriend()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null) return RedirectToAction("Login", "Account");

        var publicUsers = await _friendRequestService.GetPublicUsersAsync(currentUser.Id);
        ViewBag.PublicUsers = publicUsers.Select(u => new UserDto
        {
            Id = u.Id,
            UserName = u.UserName!,
            Email = u.Email!,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Nationality = u.Nationality,
            IsPrivate = u.IsPrivate
        }).ToList();

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddFriend(FriendRequestDto model)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null) return RedirectToAction("Login", "Account");

        if (ModelState.IsValid)
        {
            string addresseeIdentifier;

            if (!string.IsNullOrEmpty(model.SelectedUserId))
            {
                addresseeIdentifier = model.SelectedUserId;
            }
            else if (!string.IsNullOrEmpty(model.UserIdentifier))
            {
                addresseeIdentifier = model.UserIdentifier;
            }
            else
            {
                ModelState.AddModelError("", "Please select a user from the list or enter a username/email.");
                return await AddFriend();
            }

            var result = await _friendRequestService.SendFriendRequestAsync(currentUser.Id, addresseeIdentifier, model.Message);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Pending));
        }

        return await AddFriend();
    }

    public async Task<IActionResult> Pending()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null) return RedirectToAction("Login", "Account");

        var pendingRequests = await _friendRequestService.GetPendingRequestsAsync(currentUser.Id);
        var sentRequests = await _friendRequestService.GetSentRequestsAsync(currentUser.Id);

        ViewBag.SentRequests = sentRequests;
        return View(pendingRequests);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptRequest(int friendRequestId)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null) return RedirectToAction("Login", "Account");

        var result = await _friendRequestService.AcceptFriendRequestAsync(friendRequestId, currentUser.Id);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Pending));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeclineRequest(int friendRequestId)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null) return RedirectToAction("Login", "Account");

        var result = await _friendRequestService.DeclineFriendRequestAsync(friendRequestId, currentUser.Id);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Pending));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelRequest(int friendRequestId)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null) return RedirectToAction("Login", "Account");

        var result = await _friendRequestService.CancelFriendRequestAsync(friendRequestId, currentUser.Id);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Pending));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveFriend(string friendId)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null) return RedirectToAction("Login", "Account");

        var result = await _friendshipService.RemoveFriendAsync(currentUser.Id, friendId);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}