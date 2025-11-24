using System.ComponentModel.DataAnnotations;

namespace TravelHub.Domain.Entities;

public class FriendRequest
{
    public int Id { get; set; }

    public required string RequesterId { get; set; }

    public required string AddresseeId { get; set; }

    public FriendRequestStatus Status { get; set; } = FriendRequestStatus.Pending;
    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RespondedAt { get; set; }

    [MaxLength(500)]
    public string? Message { get; set; }

    // Navigation properties
    public virtual Person Requester { get; set; } = null!;
    public virtual Person Addressee { get; set; } = null!;
}