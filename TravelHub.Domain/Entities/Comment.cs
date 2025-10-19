namespace TravelHub.Domain.Entities;

public class Comment
{
    public int Id { get; set; }
    public required string Content { get; set; }
    public DateTime CreationDate { get; set; }
    public DateTime? EditDate { get; set; } // Nullable, as a comment may not be edited

    // Foreign Key for Author
    public required string AuthorId { get; set; }
    // Navigation Property to the author (Person)
    public Person? Author { get; set; }

    // Foreign Key for Post
    public required int PostId { get; set; }
    // Navigation Property back to the Post
    public Post? Post { get; set; }

    // A comment can have many photos (1:N)
    public ICollection<Photo> Photos { get; set; } = new List<Photo>();
}
