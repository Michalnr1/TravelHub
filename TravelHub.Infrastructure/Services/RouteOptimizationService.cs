using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TravelHub.Domain.DTOs;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Services;

namespace TravelHub.Infrastructure.Services;

public class RouteOptimizationService : AbstractThrottledApiService, IRouteOptimizationService
{
    private readonly string apiKey;
    private static readonly int BRUTE_FORCE_THRESHOLD = 7;

    public RouteOptimizationService(IHttpClientFactory httpClientFactory, IConfiguration config) : base(httpClientFactory, 1, TimeSpan.FromSeconds(1))
    {
        apiKey = config["ApiKeys:GoogleApiKey"]!;
    }

    // MINIMAL IMPLEMENTATION
    public async Task<List<ActivityOrder>> GetActivityOrderSuggestion(List<RouteOptimizationSpot> spots, List<RouteOptimizationActivity> otherActivities,
                                                                      RouteOptimizationSpot? start, RouteOptimizationSpot? end, List<Transport> transports, string travelMode)
    {
        int i = 0;
        foreach (RouteOptimizationSpot spot in spots)
        {
            spot.WeightMatrixIndex = i;
            i++;
        }

        double[,] weights = await GetEdgeWeights(spots, transports, travelMode);
        double[] startWeights = await GetStartWeights(start, spots, transports, travelMode);
        double[] endWeights = await GetEndWeights(end, spots, transports, travelMode);


        List<RouteOptimizationActivity> allActivities = [.. spots.Cast<RouteOptimizationActivity>(), .. otherActivities];
        allActivities = allActivities.OrderBy(a => { return a.Order; }).ToList();


        List<RouteOptimizationActivity> result = new List<RouteOptimizationActivity>();
        if (allActivities.Count <= BRUTE_FORCE_THRESHOLD)
        {
            result = RunFullSearch(allActivities, weights, startWeights, endWeights);
        }


        return ActivitiesToOrders(result, start, end);
    }

    private List<RouteOptimizationActivity> RunFullSearch(List<RouteOptimizationActivity> activities, double[,] weights, double[] startWeights, double[] endWeights)
    {
        List<RouteOptimizationActivity> best = activities;
        double bestScore = ScoreSolution(best, weights, startWeights, endWeights);

        foreach (var permutation in GetPermutations(activities))
        {
            double score = ScoreSolution(permutation, weights, startWeights, endWeights);

            if (score < bestScore)
            {
                bestScore = score;
                best = permutation;
            }
        }

        return best;
    }

    // MINIMAL IMPLEMENTATION
    private double ScoreSolution(List<RouteOptimizationActivity> activities, double[,] weights, double[] startWeights, double[] endWeights) 
    {
        List<RouteOptimizationSpot> spots = activities.Where(a => a.Type == "Spot").Cast<RouteOptimizationSpot>().ToList();
        double totalTravelTime = startWeights[spots[0].WeightMatrixIndex!.Value];
        for (int i = 0; i < spots.Count - 1; i++)
        {
            totalTravelTime += weights[spots[i].WeightMatrixIndex!.Value, spots[i+1].WeightMatrixIndex!.Value];
        }
        totalTravelTime += endWeights[spots[spots.Count - 1].WeightMatrixIndex!.Value];
        return totalTravelTime;
    }

    private List<ActivityOrder> ActivitiesToOrders(List<RouteOptimizationActivity> activities, RouteOptimizationActivity? start, RouteOptimizationActivity? end) 
    {
        int i = 1;

        List<ActivityOrder> result = [];
        if (start != null) result.Add(new ActivityOrder { ActivityId = start.Id, Order = i++ });

        foreach(RouteOptimizationActivity a in activities)
        {
            result.Add(new ActivityOrder { ActivityId = a.Id, Order = i++ });
        }

        if (end != null) result.Add(new ActivityOrder { ActivityId = end.Id, Order = i++ });

        return result;
    }

    private static IEnumerable<List<T>> GetPermutations<T>(List<T> list)
    {
        int n = list.Count;
        var result = new T[n];
        var c = new int[n];

        list.CopyTo(result, 0);
        yield return result.ToList();

        int i = 0;
        while (i < n)
        {
            if (c[i] < i)
            {
                if (i % 2 == 0)
                    (result[0], result[i]) = (result[i], result[0]);
                else
                    (result[c[i]], result[i]) = (result[i], result[c[i]]);

                yield return result.ToList();
                c[i]++;
                i = 0;
            }
            else
            {
                c[i] = 0;
                i++;
            }
        }
    }

    private async Task<double[,]> GetEdgeWeights(List<RouteOptimizationSpot> spots, List<Transport> transports, string travelMode)
    {
        List<RouteMatrixElement> routeMatrixElements = await GetRouteMatrix(spots, travelMode);
        //CHECK IF GRAPH IS CONNECTED?
        double[,] weights = new double[spots.Count, spots.Count];
        foreach (RouteMatrixElement routeMatrixElement in routeMatrixElements)
        {
            weights[routeMatrixElement.originIndex, routeMatrixElement.destinationIndex] = routeMatrixElement.duration;
        }
        foreach (Transport transport in transports)
        {
            int originIndex = spots.FindIndex(s => s.Id == transport.FromSpotId);
            int destinationIndex = spots.FindIndex(s => s.Id == transport.ToSpotId);
            if (originIndex != -1 && destinationIndex != -1)
            {
                weights[originIndex, destinationIndex] = (double) transport.Duration * 3600;
            }
        }
        return weights;
    }

    //COULD BE OPTIMIZED NOT TO MAKE REQUESTS FOR THE ROUTES THAT WE HAVE TRANSPORTS FOR?
    private async Task<double[]> GetStartWeights(RouteOptimizationSpot? start, List<RouteOptimizationSpot> spots, List<Transport> transports, string travelMode)
    {
        if (start == null) return new double[spots.Count];

        List<RouteMatrixElement> routeMatrixElements = await GetRouteMatrix(new List<RouteOptimizationSpot>() { start }, spots, travelMode);
        double[] weights = new double[spots.Count];
        foreach (RouteMatrixElement routeMatrixElement in routeMatrixElements)
        {
            weights[routeMatrixElement.destinationIndex] = routeMatrixElement.duration;
        }
        foreach (Transport transport in transports)
        {
            if (transport.FromSpotId == start.Id)
            {
                int destinationIndex = spots.FindIndex(s => s.Id == transport.ToSpotId);
                if (destinationIndex != -1)
                {
                    weights[destinationIndex] = (double)transport.Duration * 3600;
                }
            }
            
        }
        return weights;
    }

    private async Task<double[]> GetEndWeights(RouteOptimizationSpot? end, List<RouteOptimizationSpot> spots, List<Transport> transports, string travelMode)
    {
        if (end == null) return new double[spots.Count];

        List<RouteMatrixElement> routeMatrixElements = await GetRouteMatrix(spots, new List<RouteOptimizationSpot>() { end }, travelMode);
        double[] weights = new double[spots.Count];
        foreach (RouteMatrixElement routeMatrixElement in routeMatrixElements)
        {
            weights[routeMatrixElement.originIndex] = routeMatrixElement.duration;
        }
        foreach (Transport transport in transports)
        {
            if (transport.ToSpotId == end.Id)
            {
                int originIndex = spots.FindIndex(s => s.Id == transport.FromSpotId);
                if (originIndex != -1)
                {
                    weights[originIndex] = (double)transport.Duration * 3600;
                }
            }

        }
        return weights;
    }

    private async Task<List<RouteMatrixElement>> GetRouteMatrix(List<RouteOptimizationSpot> spots, string travelMode)
    {
        return await GetRouteMatrix(spots, spots, travelMode);
    }

    private async Task<List<RouteMatrixElement>> GetRouteMatrix(List<RouteOptimizationSpot> from, List<RouteOptimizationSpot> to, string travelMode)
    {
        // Safety
        if (from.Count * to.Count > 49) throw new ArgumentException();

        string url = "https://routes.googleapis.com/distanceMatrix/v2:computeRouteMatrix";
        Dictionary<string, string> headers = new Dictionary<string, string>
        {
            { "X-Goog-Api-Key", apiKey },
            { "X-Goog-FieldMask", "originIndex,destinationIndex,duration" }
        };

        var payload = new
        {
            origins = from.Select(s => { return new { waypoint = new { location = new { latLng = new { latitude = s.Latitude, longitude = s.Longitude } } } }; }).ToArray(),
            destinations = to.Select(s => { return new { waypoint = new { location = new { latLng = new { latitude = s.Latitude, longitude = s.Longitude } } } }; }).ToArray(),
            travelMode
        };

        using StringContent body = new(
            System.Text.Json.JsonSerializer.Serialize(
                payload, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull, }),
            Encoding.UTF8, "application/json");

        System.Diagnostics.Debug.WriteLine($"{await body.ReadAsStringAsync()}\n");

        using HttpResponseMessage response = await PostWithHeadersAsync(url, body, headers);

        System.Diagnostics.Debug.WriteLine($"{await response.Content.ReadAsStringAsync()}\n");

        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync();

        var rawElements = JsonSerializer.Deserialize<List<JsonElement>>(json);

        List<RouteMatrixElement> results = new();

        if (rawElements != null)
        {
            foreach (var elem in rawElements)
            {
                int originIndex = elem.GetProperty("originIndex").GetInt32();
                int destinationIndex = elem.GetProperty("destinationIndex").GetInt32();

                string durationStr = elem.GetProperty("duration").GetString() ?? "-1";

                if (durationStr.EndsWith("s"))
                    durationStr = durationStr[..^1];

                int durationSeconds = int.TryParse(durationStr, out int dur)
                                        ? dur
                                        : 0;

                results.Add(new RouteMatrixElement
                {
                    originIndex = originIndex,
                    destinationIndex = destinationIndex,
                    duration = durationSeconds
                });
            }
        }

        return results;
    }

    private record RouteMatrixElement
    {
        public int originIndex { get; set; }
        public int destinationIndex { get; set; }
        public int duration {  get; set; }
    }

}

