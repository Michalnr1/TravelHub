namespace TravelHub.Domain.Entities;

public class Post
{
    public int Id { get; set; }
    public required string Content { get; set; }
    public DateTime CreationDate { get; set; }
    public DateTime? EditDate { get; set; } // Nullable, as a post may not be edited

    // Foreign Key for Author
    public required string AuthorId { get; set; }
    // Navigation Property to the author (Person)
    public Person? Author { get; set; }

    // A post can have many comments
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    
    // A post can have many photos (1:N)
    public ICollection<Photo> Photos { get; set; } = new List<Photo>();
}
