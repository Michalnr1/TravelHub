namespace TravelHub.Domain.Entities;

public class PersonFriends
{
    public string UserId { get; set; } = string.Empty;
    public string FriendId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public virtual Person User { get; set; } = null!;
    public virtual Person Friend { get; set; } = null!;
}