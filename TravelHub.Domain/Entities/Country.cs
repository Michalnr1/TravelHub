using System.ComponentModel.DataAnnotations;

namespace TravelHub.Domain.Entities;

public class Country
{
    [Key]
    public required string Code { get; set; }
    [Key]
    public required string Name { get; set; }

    public ICollection<Spot> Spots { get; set; } = new List<Spot>();
}
