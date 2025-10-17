using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelHub.Domain.Entities;

public class Accommodation
{
    // The PK is also the FK to Spot, creating a 1:1 relationship
    [Key, ForeignKey("Spot")]
    public int Id { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public decimal CheckInTime { get; set; }
    public decimal CheckOutTime { get; set; }

    // Navigation Property back to the Spot
    public Spot Spot { get; set; }
}
