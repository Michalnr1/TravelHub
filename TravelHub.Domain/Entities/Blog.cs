namespace TravelHub.Domain.Entities;

public class Blog
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Catalog { get; set; }

    // Foreign Key for Owner
    public required string OwnerId { get; set; }
    // Navigation Property to the owner (Person)
    public Person? Owner { get; set; }

    // Foreign Key for Owner
    public int TripId { get; set; }
    // Navigation Property to the owner (Person)
    public Trip? Trip { get; set; }

    // A blog can have multiple posts (1:N)
    public ICollection<Post> Posts { get; set; } = new List<Post>();
}
