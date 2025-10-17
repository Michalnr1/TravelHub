using System.ComponentModel.DataAnnotations.Schema;

namespace TravelHub.Domain.Entities;

public class Spot
{
    public int Id { get; set; }
    public double Longitude { get; set; }
    public double Latitude { get; set; }
    public decimal Cost { get; set; }

    // Foreign Key for Activity
    public int ActivityId { get; set; }
    // Navigation Property back to the Activity
    public Activity Activity { get; set; }

    // Navigation Properties

    // A spot is associated with one accommodation (1:1)
    public Accommodation Accommodation { get; set; }

    // A spot can have many photos (1:N)
    public ICollection<Photo> Photos { get; set; } = new List<Photo>();

    // A spot can be the starting point for many transports
    [InverseProperty("FromSpot")]
    public ICollection<Transport> TransportsFrom { get; set; } = new List<Transport>();

    // A spot can be the destination for many transports
    [InverseProperty("ToSpot")]
    public ICollection<Transport> TransportsTo { get; set; } = new List<Transport>();
}
