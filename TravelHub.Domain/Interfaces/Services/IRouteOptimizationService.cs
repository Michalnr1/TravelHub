using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TravelHub.Domain.DTOs;
using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Services;

public interface IRouteOptimizationService
{
    Task<List<ActivityOrder>> GetActivityOrderSuggestion(List<RouteOptimizationSpot> spots, List<RouteOptimizationActivity> otherActivities, RouteOptimizationSpot? start, RouteOptimizationSpot? end, List<Transport> transports, string travelMode);
}
