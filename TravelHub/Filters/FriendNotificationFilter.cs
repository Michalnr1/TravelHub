using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Services;

namespace TravelHub.Web.Filters;

public class FriendNotificationFilter : IAsyncActionFilter
{
    private readonly IFriendRequestService _friendRequestService;
    private readonly UserManager<Person> _userManager;

    public FriendNotificationFilter(
        IFriendRequestService friendRequestService,
        UserManager<Person> userManager)
    {
        _friendRequestService = friendRequestService;
        _userManager = userManager;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Sprawdź czy użytkownik jest zalogowany
        if (context.HttpContext.User.Identity?.IsAuthenticated == true)
        {
            try
            {
                // Pobierz zalogowanego użytkownika
                var user = await _userManager.GetUserAsync(context.HttpContext.User);
                if (user != null)
                {
                    // Pobierz liczbę oczekujących requestów
                    var pendingRequests = await _friendRequestService.GetPendingRequestsAsync(user.Id);

                    // Przekaż do ViewBag - dostępne we WSZYSTKICH widokach
                    if (context.Controller is Controller controller)
                    {
                        controller.ViewBag.PendingRequestsCount = pendingRequests.Count;
                    }

                    // Możesz też dodać do ViewData lub HttpContext.Items
                    context.HttpContext.Items["PendingRequestsCount"] = pendingRequests.Count;
                }
            }
            catch (Exception ex)
            {
                // Logowanie błędów - nie przerywamy działania aplikacji
                // Możesz dodać logger: _logger.LogError(ex, "Error loading friend notifications");
                Console.WriteLine($"Error in FriendNotificationFilter: {ex.Message}");
            }
        }
        else
        {
            // Użytkownik nie jest zalogowany - ustaw 0
            if (context.Controller is Controller controller)
            {
                controller.ViewBag.PendingRequestsCount = 0;
            }
        }

        // Kontynuuj wykonanie akcji
        await next();
    }
}