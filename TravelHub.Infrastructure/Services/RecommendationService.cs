using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TravelHub.Domain.DTOs;
using TravelHub.Domain.Interfaces.Services;

namespace TravelHub.Infrastructure.Services;

public class RecommendationService : AbstractThrottledApiService, IRecommendationService
{

    private readonly string apiKey;

    public RecommendationService(IHttpClientFactory httpClientFactory, IConfiguration config) : base(httpClientFactory, 1, TimeSpan.FromSeconds(1))
    {
        apiKey = config["ApiKeys:GoogleApiKey"]!;
    }

    public async Task<List<AttractionDto>> FindRecommendationsByCoords(double lat, double lng, int distanceMeters)
    {
        string url = "https://places.googleapis.com/v1/places:searchNearby";
        Dictionary<string, string> headers = new Dictionary<string, string>
        {
            { "X-Goog-Api-Key", apiKey },
            { "X-Goog-FieldMask", "places.displayName,places.location,places.photos" }

        };

        var payload = new
        {
            includedTypes = new[] { "tourist_attraction" },
            maxResultCount = 20,
            locationRestriction = new
            {
                circle = new
                {
                    center = new
                    {
                        latitude = lat,
                        longitude = lng
                    },
                    radius = distanceMeters
                }
            }
        };

        using StringContent body = new(
            System.Text.Json.JsonSerializer.Serialize(
                payload, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull, }),
            Encoding.UTF8, "application/json");

        System.Diagnostics.Debug.WriteLine($"{await body.ReadAsStringAsync()}\n");

        using HttpResponseMessage response = await PostWithHeadersAsync(url, body, headers);

        response.EnsureSuccessStatusCode();

        if (response != null)
        {
            string jsonString = await response.Content.ReadAsStringAsync();
            object json = JsonConvert.DeserializeObject<object>(jsonString);
            JObject joResponse = JObject.Parse(jsonString);
            JArray dataArray = (JArray)joResponse["places"]!;
            System.Diagnostics.Debug.WriteLine($"{await response.Content.ReadAsStringAsync()}\n");
            var places = dataArray
                .Where((place) =>
                {
                    return place["displayName"]?["text"] != null
                    && place["location"]?["latitude"] != null
                    && place["location"]?["longitude"] != null;
                })
                .Select((place) =>
                {
                    return new AttractionDto
                    {
                        Name =  place["displayName"]?["text"].ToString()!,
                        Latitude = double.Parse(place["location"]?["latitude"].ToString()!),
                        Longitude = double.Parse(place["location"]?["longitude"].ToString()!),
                        PhotoName = place["photos"]?[0]?["name"] != null ? place["photos"]?[0]?["name"].ToString() : null,
                        PhotoAuthor = place["photos"]?[0]?["authorAttributions"]?[0]?["displayName"] != null ? place["photos"]?[0]?["authorAttributions"]?[0]?["displayName"].ToString() : null,
                    };
                })
                .ToList();

            return places;
        }

        return new List<AttractionDto>();
    }

    public async Task<string> GetPhotoUrl(string photoName)
    {
        string url = $"https://places.googleapis.com/v1/{photoName}/media";
        Dictionary<string, string?> param = new Dictionary<string, string?>() { { "key", apiKey }, { "maxHeightPx", "200" }};

        Uri newUrl = new Uri(QueryHelpers.AddQueryString(url, param));
        HttpResponseMessage response = await GetAsync(newUrl.ToString());
        if (response != null)
        {
            string finalUrl = response.RequestMessage!.RequestUri!.ToString();
            return finalUrl;
        } else
        {
            return "";
        }
        
    }
}
