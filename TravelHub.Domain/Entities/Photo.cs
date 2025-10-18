namespace TravelHub.Domain.Entities;

public class Photo
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Alt { get; set; } // Alt text for the photo
    // public Rating Rating { get; set; }

    // Foreign Key for Spot
    public int SpotId { get; set; }
    // Navigation Property back to the spot
    public Spot Spot { get; set; }

    // Foreign Key for Post (Opcjonalna)
    public int? PostId { get; set; }
    // Navigation Property back to the post
    public Post Post { get; set; }

    // Foreign Key for Comment (Opcjonalna)
    public int? CommentId { get; set; }
    // Navigation Property back to the comment
    public Comment Comment { get; set; }
}
