using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Services;

namespace TravelHub.Infrastructure.Services;

public class ReverseGeocodingService : AbstractThrottledApiService, IReverseGeocodingService
{
    public ReverseGeocodingService(IHttpClientFactory httpClientFactory, IConfiguration config)
            : base(httpClientFactory,
                   maxRequestsPerWindow: int.Parse(config["Nominatim:RateLimit:MaxRequests"] ?? "1"),
                   timeWindow: TimeSpan.FromSeconds(double.Parse(config["Nominatim:RateLimit:WindowSeconds"] ?? "1")))
    {
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "TravelHub");
    }

    public async Task<(string?, string?, string?)> GetCountryAndCity(double lat, double lng)
    {
        const string url = "https://nominatim.openstreetmap.org/reverse";
        Dictionary<string, string?> param = new Dictionary<string, string?>() { { "lat", lat.ToString() }, { "lon", lng.ToString() }, { "format", "json" },
                                                       { "zoom", "12" }, { "accept-language", "en" }};

        Uri newUrl = new Uri(QueryHelpers.AddQueryString(url, param));
        HttpResponseMessage response = await GetAsync(newUrl.ToString());
        if (response != null)
        {
            string jsonString = await response.Content.ReadAsStringAsync();
            object json = JsonConvert.DeserializeObject<object>(jsonString);
            JObject joResponse = JObject.Parse(jsonString);
            JObject address = (JObject)joResponse["address"];
            if (address != null)
            {
                string? countryName = address["country"] != null ? address["country"].ToString() : null;
                string? countryCode = address["country_code"] != null ? address["country_code"].ToString() : null;
                string? city = address["city"] != null ? address["city"].ToString() : null;
                string? town = address["town"] != null ? address["town"].ToString() : null;

                return (countryName, countryCode, city ?? town);
            }
        }
        return (null, null, null);

    }
}
