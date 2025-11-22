using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelHub.Domain.DTOs;

public class UpdateActivityOrderRequest
{
    public int DayId { get; set; }
    public List<ActivityOrder> Activities { get; set; } = new();
}

public class ActivityOrder
{
    public int ActivityId { get; set; }
    public int Order { get; set; }
}

public record RouteOptimizationActivity
{
    public int Id { get; set; }
    public decimal Duration { get; set; }
    public int Order { get; set; }
    public decimal? StartTime { get; set; }
    public required string Type { get; set; } //Activity or Spot
}

public record RouteOptimizationSpot : RouteOptimizationActivity
{
    public double Longitude { get; set; }
    public double Latitude { get; set; }
    public int? WeightMatrixIndex { get; set; }
}

public record RouteOptimizationTransport
{
    public int FromSpotId { get; set; }
    public int ToSpotId { get; set; }
    public decimal Duration { get; set; }
}
