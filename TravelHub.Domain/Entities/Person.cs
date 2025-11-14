using Microsoft.AspNetCore.Identity;

namespace TravelHub.Domain.Entities;

public class Person : IdentityUser
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Nationality { get; set; }
    public DateTime Birthday { get; set; }
    public required bool IsPrivate { get; set; } = true;
    public string? DefaultAirportCode { get; set; }

    // Navigation Properties

    // A person can have many friends, and be a friend to many others (M:N self-referencing)
    public ICollection<PersonFriends> Friends { get; set; } = new List<PersonFriends>();
    public ICollection<PersonFriends> FriendOf { get; set; } = new List<PersonFriends>();

    // A person can organize many trips (1:N)
    public ICollection<Trip> Trips { get; set; } = new List<Trip>();

    // A person can be the author of many posts (1:N)
    public ICollection<Post> Posts { get; set; } = new List<Post>();

    // A person can be the author of many comments (1:N)
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();

    // A person can pay for many expenses (1:N)
    public ICollection<Expense> PaidExpenses { get; set; } = new List<Expense>();

    // A person can recive many transfers (1:N)
    public ICollection<Expense> RecivedTransfers { get; set; } = new List<Expense>();

    // An expense can be for many people, and a person can be part of many expenses (M:N)
    // public ICollection<Expense> ExpensesToCover { get; set; } = new List<Expense>();
    public ICollection<ExpenseParticipant> ExpensesToCover { get; set; } = new List<ExpenseParticipant>();
    // A person can have many notifications (1:N)
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    // A person can have many chat messages (1:N)
    public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

    // A person can have many blogs (1:N)
    public ICollection<Blog> Blogs { get; set; } = new List<Blog>();
}
