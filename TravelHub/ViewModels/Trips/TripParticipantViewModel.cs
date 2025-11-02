using System.ComponentModel.DataAnnotations;
using TravelHub.Domain.Entities;

namespace TravelHub.Web.ViewModels.Trips;

public class TripParticipantViewModel
{
    public int Id { get; set; }
    public string PersonId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public TripParticipantStatus Status { get; set; }
    public DateTime JoinedAt { get; set; }
    public bool IsOwner { get; set; }
}

public class AddParticipantViewModel
{
    public int TripId { get; set; }
    public string TripName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a friend to add")]
    [Display(Name = "Select Friend")]
    public string SelectedFriendId { get; set; } = string.Empty;
    public List<FriendViewModel> AvailableFriends { get; set; } = new();
}

public class FriendViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class MyTripsViewModel
{
    public List<TripViewModel> OwnedTrips { get; set; } = new();
    public List<TripViewModel> ParticipatingTrips { get; set; } = new();
    public List<TripViewModel> PendingInvitations { get; set; } = new();
}