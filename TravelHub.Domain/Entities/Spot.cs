using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelHub.Domain.Entities;

public class Spot : Activity
{
    public double Longitude { get; set; }
    public double Latitude { get; set; }
    // public decimal Cost { get; set; }
    public Rating? Rating { get; set; }

    public string? FileName { get; set; }

    // Navigation Properties

    [InverseProperty("Spot")]
    public Expense? Expense { get; set; }

    public string? CountryCode { get; set; }
    public string? CountryName { get; set; }
    public Country? Country { get; set; }

    // A spot can have many photos (1:N)
    public ICollection<Photo> Photos { get; set; } = new List<Photo>();

    // A spot can be the starting point for many transports
    [InverseProperty("FromSpot")]
    public ICollection<Transport> TransportsFrom { get; set; } = new List<Transport>();

    // A spot can be the destination for many transports
    [InverseProperty("ToSpot")]
    public ICollection<Transport> TransportsTo { get; set; } = new List<Transport>();   
}
