using TravelHub.Domain.Entities;

namespace TravelHub.Domain.DTOs;

public class FriendRequestDto
{
    public string? UserIdentifier { get; set; }
    public string? SelectedUserId { get; set; }
    public string? Message { get; set; }
}

public class FriendRequestResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public FriendRequest? FriendRequest { get; set; }
}

public class FriendshipResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Nationality { get; set; } = string.Empty;
    public bool IsPrivate { get; set; }
}