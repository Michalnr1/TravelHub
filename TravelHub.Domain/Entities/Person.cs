using Microsoft.AspNetCore.Identity;

namespace TravelHub.Domain.Entities;

public class Person : IdentityUser
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Nationality { get; set; }
    public DateTime Birthday { get; set; }

    // Navigation Properties

    // A person can have many friends, and be a friend to many others (M:N self-referencing)
    public ICollection<Person> Friends { get; set; } = new List<Person>();
    public ICollection<Person> FriendOf { get; set; } = new List<Person>();

    // A person can organize many trips (1:N)
    public ICollection<Trip> Trips { get; set; } = new List<Trip>();

    // A person can be the author of many posts (1:N)
    public ICollection<Post> Posts { get; set; } = new List<Post>();

    // A person can be the author of many comments (1:N)
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();

    // A person can pay for many expenses (1:N)
    public ICollection<Expense> PaidExpenses { get; set; } = new List<Expense>();

    // An expense can be for many people, and a person can be part of many expenses (M:N)
    public ICollection<Expense> ExpensesToCover { get; set; } = new List<Expense>();
}
