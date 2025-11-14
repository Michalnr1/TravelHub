namespace TravelHub.Domain.Entities;

public class Post
{
    public int Id { get; set; }
    public required string Content { get; set; }
    public required string Title { get; set; }
    public DateTime CreationDate { get; set; }
    public DateTime? EditDate { get; set; } // Nullable, as a post may not be edited

    // Foreign Key for Author
    public required string AuthorId { get; set; }
    // Navigation Property to the author (Person)
    public Person? Author { get; set; }

    // Foreign Key for Blog
    public int BlogId { get; set; }
    // Navigation Property to the blog
    public Blog? Blog { get; set; }

    // Foreign Key for Day
    public int? DayId { get; set; }
    // Navigation Property to the day
    public Day? Day { get; set; }

    // A post can have many comments
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    
    // A post can have many photos (1:N)
    public ICollection<Photo> Photos { get; set; } = new List<Photo>();
}
