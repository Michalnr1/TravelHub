namespace TravelHub.Domain.Entities;

public class Comment
{
    public int Id { get; set; }
    public string Content { get; set; }
    public DateTime CreationDate { get; set; }
    public DateTime? EditDate { get; set; } // Nullable, as a comment may not be edited

    // Foreign Key for Author
    public int AuthorId { get; set; }
    // Navigation Property to the author (Person)
    public Person Author { get; set; }

    // Foreign Key for Post
    public int PostId { get; set; }
    // Navigation Property back to the Post
    public Post Post { get; set; }
}
