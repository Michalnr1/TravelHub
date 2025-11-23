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
                                                                      RouteOptimizationSpot? start, RouteOptimizationSpot? end, List<RouteOptimizationTransport> transports, 
                                                                      string travelMode, decimal startTime)
    {
        int i = 0;
        foreach (RouteOptimizationSpot spot in spots)
        {
            spot.WeightMatrixIndex = i;
            i++;
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        double[,] weights = await GetEdgeWeights(spots, transports, travelMode);
        double[] startWeights = await GetStartWeights(start, spots, transports, travelMode);
        double[] endWeights = await GetEndWeights(end, spots, transports, travelMode);
        sw.Stop();
        System.Diagnostics.Debug.WriteLine($"API calls took {sw.ElapsedMilliseconds}\n");

        List<RouteOptimizationActivity> allActivities = [.. spots.Cast<RouteOptimizationActivity>(), .. otherActivities];
        allActivities = allActivities.OrderBy(a => { return a.Order; }).ToList();

        sw = System.Diagnostics.Stopwatch.StartNew();
        List<RouteOptimizationActivity> result = new List<RouteOptimizationActivity>();
        if (allActivities.Count <= BRUTE_FORCE_THRESHOLD)
        {
            result = RunFullSearch(allActivities, weights, startWeights, endWeights, startTime);
        }
        sw.Stop();
        System.Diagnostics.Debug.WriteLine($"Optimization took {sw.ElapsedMilliseconds}\n");


        return ActivitiesToOrders(result, start, end);
    }

    private List<RouteOptimizationActivity> RunFullSearch(List<RouteOptimizationActivity> activities, double[,] weights, double[] startWeights, double[] endWeights, decimal startTime)
    {
        List<RouteOptimizationActivity> best = activities;
        double bestScore = ScoreSolution(best, weights, startWeights, endWeights, startTime);

        foreach (var permutation in GetPermutations(activities))
        {
            double score = ScoreSolution(permutation, weights, startWeights, endWeights, startTime);

            if (score < bestScore)
            {
                bestScore = score;
                best = permutation;
            }
        }

        return best;
    }

    // MINIMAL IMPLEMENTATION
    private double ScoreSolution(List<RouteOptimizationActivity> activities, double[,] weights, double[] startWeights, double[] endWeights, decimal startTime) 
    {
        double time = (double)startTime;
        int spotIdx = 0;
        double latePenalty = 0;
        List<RouteOptimizationSpot> spots = activities.Where(a => a.Type == "Spot").Cast<RouteOptimizationSpot>().ToList();
        foreach (RouteOptimizationActivity activity in activities)
        {
            if (activity.Type == "Spot")
            {
                RouteOptimizationSpot spot = (RouteOptimizationSpot)activity;
                if (spotIdx == 0)
                {
                    time += startWeights[spot.WeightMatrixIndex!.Value] / 3600;
                }
                else
                {
                    
                    time += weights[spots[spotIdx - 1].WeightMatrixIndex!.Value, spots[spotIdx].WeightMatrixIndex!.Value] / 3600;
                }
                spotIdx++;
            }
            if (activity.StartTime != null && activity.StartTime > 0)
            {
                if ((double)activity.StartTime < time)
                {
                    // Additional penalty for time being late - if being late is unavoidable, shorter lateness should be preferred
                    latePenalty += (double)(time - (double)activity.StartTime) * 1;
                }
                else
                {
                    time = (double)activity.StartTime.Value;
                }
            }
            time += (double)activity.Duration;
            if (spotIdx == spots.Count - 1)
            {
                time += endWeights[spots[spotIdx].WeightMatrixIndex!.Value] / 3600;
            }
        }
        if (latePenalty > 0)
        {
            latePenalty += 24; //Large penalty for being late, it should significantly outsize regular travel time penalty 
        }
        double totalTime = time - (double)startTime;
        return totalTime + latePenalty;
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

    private async Task<double[,]> GetEdgeWeights(List<RouteOptimizationSpot> spots, List<RouteOptimizationTransport> transports, string travelMode)
    {
        int limit = travelMode == "TRANSIT" ? 10 : 25;
        double[,] weights = new double[spots.Count, spots.Count];
        for (int i = 0; i < spots.Count; i++)
        {
            for (int j = 0; j < spots.Count; j++)
            {
                weights[i,j] = double.PositiveInfinity;
            }
        }
        if (spots.Count <= limit)
        {
            List<RouteMatrixElement> routeMatrixElements = await GetRouteMatrix(spots, travelMode);
            //CHECK IF GRAPH IS CONNECTED?
            foreach (RouteMatrixElement routeMatrixElement in routeMatrixElements)
            {
                if (routeMatrixElement.duration > 0)
                    weights[routeMatrixElement.originIndex, routeMatrixElement.destinationIndex] = routeMatrixElement.duration;
            }
        }
        else
        {
            List<List<RouteOptimizationSpot>> sublists = new List<List<RouteOptimizationSpot>>();
            int k = 0;
            while (k < spots.Count)
            {
                sublists.Add(spots.Slice(k, Math.Min(limit, spots.Count - k)));
                k += limit;
            }
            for (int i = 0; i < sublists.Count; i++)
            {
                for (int j = 0; j < sublists.Count; j++)
                {
                    List<RouteMatrixElement> routeMatrixElements = await GetRouteMatrix(sublists[i], sublists[j], travelMode);
                    foreach (RouteMatrixElement routeMatrixElement in routeMatrixElements)
                    {
                        weights[routeMatrixElement.originIndex + i * limit, routeMatrixElement.destinationIndex + j * limit] = routeMatrixElement.duration;
                    }
                }
            }
        }
        foreach (RouteOptimizationTransport transport in transports)
        {
            int originIndex = spots.FindIndex(s => s.Id == transport.FromSpotId);
            int destinationIndex = spots.FindIndex(s => s.Id == transport.ToSpotId);
            if (originIndex != -1 && destinationIndex != -1)
            {
                weights[originIndex, destinationIndex] = (double)transport.Duration * 3600;
            }
        }
        return weights;



    }

    //COULD BE OPTIMIZED NOT TO MAKE REQUESTS FOR THE ROUTES THAT WE HAVE TRANSPORTS FOR?
    private async Task<double[]> GetStartWeights(RouteOptimizationSpot? start, List<RouteOptimizationSpot> spots, List<RouteOptimizationTransport> transports, string travelMode)
    {
        if (start == null) return new double[spots.Count];

        List<RouteMatrixElement> routeMatrixElements = await GetRouteMatrix(new List<RouteOptimizationSpot>() { start }, spots, travelMode);
        double[] weights = new double[spots.Count];
        for (int i = 0; i < spots.Count; i++)
        {
            weights[i] = double.PositiveInfinity;
        }
        foreach (RouteMatrixElement routeMatrixElement in routeMatrixElements)
        {
            if (routeMatrixElement.duration > 0)
                weights[routeMatrixElement.destinationIndex] = routeMatrixElement.duration;
        }
        foreach (RouteOptimizationTransport transport in transports)
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

    private async Task<double[]> GetEndWeights(RouteOptimizationSpot? end, List<RouteOptimizationSpot> spots, List<RouteOptimizationTransport> transports, string travelMode)
    {
        if (end == null) return new double[spots.Count];

        List<RouteMatrixElement> routeMatrixElements = await GetRouteMatrix(spots, new List<RouteOptimizationSpot>() { end }, travelMode);
        double[] weights = new double[spots.Count];
        for (int i = 0; i < spots.Count; i++)
        {
            weights[i] = double.PositiveInfinity;
        }
        foreach (RouteMatrixElement routeMatrixElement in routeMatrixElements)
        {
            if (routeMatrixElement.duration > 0)
                weights[routeMatrixElement.originIndex] = routeMatrixElement.duration;
        }
        foreach (RouteOptimizationTransport transport in transports)
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

                string durationStr = "-1";
                try
                {
                    durationStr = elem.GetProperty("duration").GetString() ?? "-1";
                } catch (KeyNotFoundException)
                {

                }
                

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

