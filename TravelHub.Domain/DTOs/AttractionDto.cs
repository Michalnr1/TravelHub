using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelHub.Domain.DTOs;

public record AttractionDto
{
    public required string Name { get; set; }
    public required double Latitude { get; set; }
    public required double Longitude { get; set; }
    public string? PhotoName { get; set; }
    public string? PhotoAuthor { get; set; }
}
