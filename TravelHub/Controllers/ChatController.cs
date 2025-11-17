using Microsoft.AspNetCore.Mvc;
using TravelHub.Domain.DTOs;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Web.ViewModels.Trips;

namespace TravelHub.Web.Controllers
{
    /// <summary>
    /// Controller for trip chat actions. Maps domain entities to view models for the UI.
    /// </summary>
    public class ChatController : Controller
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        public async Task<IActionResult> Index(int tripId)
        {
            var messagesDomain = await _chatService.GetMessagesForTripAsync(tripId);

            // Map domain entities to view models for the view
            var messagesVm = messagesDomain.Select(m => new ChatMessageViewModel
            {
                Id = m.Id,
                Message = m.Message,
                PersonId = m.PersonId,
                PersonFirstName = m.Person?.FirstName ?? string.Empty,
                PersonLastName = m.Person?.LastName ?? string.Empty,
                TripId = m.TripId
            }).ToList();

            var vm = new ChatViewModel
            {
                TripId = tripId,
                Messages = messagesVm,
                CurrentPersonId = GetCurrentPersonId()
            };

            ViewBag.CurrentPersonId = GetCurrentPersonId();

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostMessage(int tripId, ChatMessageCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                var messagesDomain = await _chatService.GetMessagesForTripAsync(tripId);
                var messagesVm = messagesDomain.Select(m => new ChatMessageViewModel
                {
                    Id = m.Id,
                    Message = m.Message,
                    PersonId = m.PersonId,
                    PersonFirstName = m.Person?.FirstName ?? string.Empty,
                    PersonLastName = m.Person?.LastName ?? string.Empty,
                    TripId = m.TripId
                }).ToList();

                var vm = new ChatViewModel
                {
                    TripId = tripId,
                    Messages = messagesVm,
                    NewMessage = dto,
                    CurrentPersonId = GetCurrentPersonId()
                };

                ViewBag.CurrentPersonId = GetCurrentPersonId();

                return View("Index", vm);
            }

            var personId = GetCurrentPersonId();
            try
            {
                var createdDomain = await _chatService.CreateMessageAsync(tripId, dto, personId);

                return RedirectToAction(nameof(Index), new { tripId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);

                var messagesDomain = await _chatService.GetMessagesForTripAsync(tripId);
                var messagesVm = messagesDomain.Select(m => new ChatMessageViewModel
                {
                    Id = m.Id,
                    Message = m.Message,
                    PersonId = m.PersonId,
                    PersonFirstName = m.Person?.FirstName ?? string.Empty,
                    PersonLastName = m.Person?.LastName ?? string.Empty,
                    TripId = m.TripId
                }).ToList();

                var vm = new ChatViewModel
                {
                    TripId = tripId,
                    Messages = messagesVm,
                    NewMessage = dto,
                    CurrentPersonId = personId
                };

                ViewBag.CurrentPersonId = personId;

                return View("Index", vm);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int messageId, int tripId)
        {
            try
            {
                await _chatService.DeleteMessageAsync(messageId, GetCurrentPersonId());
                return RedirectToAction(nameof(Index), new { tripId });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        private string? GetCurrentPersonId()
        {
            var claim = User?.FindFirst("personId") ?? User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return claim?.Value;
        }

        // GET: /Chat/MessagesPartial?tripId=123
        public async Task<IActionResult> MessagesPartial(int? tripId)
        {
            ViewBag.CurrentPersonId = GetCurrentPersonId();

            if (!tripId.HasValue)
            {
                // return empty partial if tripId not provided
                var emptyList = Enumerable.Empty<ChatMessageViewModel>();
                return PartialView("_ChatMessagesPartial", emptyList);
            }

            try
            {
                var messagesDomain = await _chatService.GetMessagesForTripAsync(tripId.Value);

                // Map domain ChatMessage -> ChatMessageViewModel
                var messagesVm = messagesDomain.Select(m => new ChatMessageViewModel
                {
                    Id = m.Id,
                    Message = m.Message,
                    PersonId = m.PersonId,
                    PersonFirstName = m.Person?.FirstName ?? string.Empty,
                    PersonLastName = m.Person?.LastName ?? string.Empty,
                    TripId = m.TripId
                }).ToList();

                return PartialView("_ChatMessagesPartial", messagesVm);
            }
            catch (Exception)
            {
                return PartialView("_ChatMessagesPartial", Enumerable.Empty<ChatMessageViewModel>());
            }
        }

    }
}
