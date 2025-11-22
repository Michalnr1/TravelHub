using System.ComponentModel.DataAnnotations;
using TravelHub.Domain.DTOs;

namespace TravelHub.Web.ViewModels.Trips;

// ViewModel used to render messages in the UI
public class ChatMessageViewModel
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public string PersonId { get; set; } = string.Empty;
    public string PersonFirstName { get; set; } = string.Empty;
    public string PersonLastName { get; set; } = string.Empty;
    public int TripId { get; set; }
}

// Aggregated view model for the chat page
public class ChatViewModel
{
    public int TripId { get; set; }
    public List<ChatMessageViewModel> Messages { get; set; } = new();
    public ChatMessageCreateDto NewMessage { get; set; } = new();
    public string? CurrentPersonId { get; set; }
}
