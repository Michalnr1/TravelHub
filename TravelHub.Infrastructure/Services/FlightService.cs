using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TravelHub.Domain.DTOs;
using TravelHub.Domain.Interfaces.Services;

namespace TravelHub.Infrastructure.Services;

public class FlightService : AbstractThrottledApiService, IFlightService
{
    private readonly string apiId;
    private readonly string apiSecret;
    private string? apiAccessToken;
    private DateTime tokenExpiration;
    public FlightService(IHttpClientFactory httpClientFactory, IConfiguration config) : base(httpClientFactory, maxRequestsPerWindow: int.Parse(config["Amadeus:RateLimit:MaxRequests"] ?? "1"),
                   timeWindow: TimeSpan.FromSeconds(double.Parse(config["Amadeus:RateLimit:WindowSeconds"] ?? "0.1", CultureInfo.InvariantCulture)))
    {
        apiId = config["Amadeus:Id"]!;
        apiSecret = config["Amadeus:Secret"]!;
        tokenExpiration = DateTime.MinValue;
    }

    public async Task<List<FlightDto>> GetFlights(string fromAirportCode, string toAirportCode, DateTime departureDate, 
                                        DateTime? returnDate = null, int? adults = 1, int? children = 0, int? seated_infants = 0, int? held_infants = 0)
    {
        if (DateTime.Now > tokenExpiration)
        {
            await UpdateAccessToken();
        }

        string url = "https://test.api.amadeus.com/v2/shopping/flight-offers";

        List<TravellerDto> travelers;
        try
        {
            travelers = BuildTravelersList(adults ?? 1, children ?? 0, seated_infants ?? 0, held_infants ?? 0);
        } catch (ArgumentException)
        {
            return new List<FlightDto>();
        }
        

        var payload = new
            {
                originDestinations = new[]
                    {
                        new
                        {
                            id = "1",
                            originLocationCode = fromAirportCode,
                            destinationLocationCode = toAirportCode,
                            departureDateTimeRange = new
                            {
                                date = departureDate.Date.ToString("yyyy-MM-dd")
                            },
                            //destinationRadius = 299
                        }
                    },
                travelers = travelers.Select((traveler) =>
                {
                    return new
                        {
                            id = traveler.Id,
                            travelerType = traveler.Type,
                            associatedAdultId = traveler.AssociatedAdultId
                        };
                }),
                sources = new[] { "GDS" }
                // currencyCode = "EUR"
            };

        using StringContent body = new(
            System.Text.Json.JsonSerializer.Serialize(
                payload, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull, }), 
            Encoding.UTF8, "application/json");

        System.Diagnostics.Debug.WriteLine($"{await body.ReadAsStringAsync()}\n");

        using HttpResponseMessage response = await PostWithHeadersAsync(
            url,
            body,
            new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {apiAccessToken}" },
                { "X-HTTP-Method-Override", "GET" }
            });

        response.EnsureSuccessStatusCode();     

        if (response != null)
        {
            string jsonString = await response.Content.ReadAsStringAsync();
            object json = JsonConvert.DeserializeObject<object>(jsonString);
            JObject joResponse = JObject.Parse(jsonString);
            JArray dataArray = (JArray)joResponse["data"]!;

            var flights = dataArray
                .Select(FlightOfferToFlightDto)
                .ToList();

            return flights;
        }

        return new List<FlightDto>();
    }

    public async Task<AirportDto?> GetAirportByCoords(double lat, double lng)
    {

        if (DateTime.Now > tokenExpiration)
        {
            await UpdateAccessToken();
        }

        string url = "https://test.api.amadeus.com/v1/reference-data/locations/airports";
        Dictionary<string, string?> param = new Dictionary<string, string?>() { { "latitude", lat.ToString() }, { "longitude", lng.ToString() }};
        Uri newUrl = new Uri(QueryHelpers.AddQueryString(url, param));
        using HttpResponseMessage response = await GetWithHeadersAsync(
            newUrl.ToString(),
            new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {apiAccessToken}" }
            });
        System.Diagnostics.Debug.WriteLine($"{await response.Content.ReadAsStringAsync()}\n");
        response.EnsureSuccessStatusCode();

        if (response != null)
        {
            string jsonString = await response.Content.ReadAsStringAsync();
            object json = JsonConvert.DeserializeObject<object>(jsonString);
            JObject joResponse = JObject.Parse(jsonString);
            JArray dataArray = (JArray)joResponse["data"]!;
            if (dataArray.Count == 0) return null;
            JObject firstResult = (JObject)dataArray[0];

            AirportDto airport = new AirportDto()
            {
                AirportCode = firstResult["iataCode"].ToString(),
                AirportName = firstResult["name"].ToString(),
                CityName = firstResult["address"]?["cityName"].ToString(),
                CountryName = firstResult["address"]?["countryName"].ToString(),
                Latitude = firstResult["geoCode"]?["latitude"] != null ? double.Parse(firstResult["geoCode"]?["latitude"].ToString()!) : null,
                Longitude = firstResult["geoCode"]?["longitude"] != null ? double.Parse(firstResult["geoCode"]?["longitude"].ToString()!) : null,
                Distance = firstResult["distance"]?["value"] != null ? int.Parse(firstResult["distance"]?["value"].ToString()!) : null,
                DistanceUnit = firstResult["distance"]?["unit"].ToString()
            };    

            return airport;
        }
        return null;

    }

    public async Task<List<AirportDto>> GetAirportsByName(string query)
    {
        if (DateTime.Now > tokenExpiration)
        {
            await UpdateAccessToken();
        }

        string url = "https://test.api.amadeus.com/v1/reference-data/locations";
        Dictionary<string, string?> param = new Dictionary<string, string?>() { { "subType", "AIRPORT" }, { "keyword", query } };
        Uri newUrl = new Uri(QueryHelpers.AddQueryString(url, param));
        using HttpResponseMessage response = await GetWithHeadersAsync(
            newUrl.ToString(),
            new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {apiAccessToken}" }
            });
        System.Diagnostics.Debug.WriteLine($"{await response.Content.ReadAsStringAsync()}\n");
        response.EnsureSuccessStatusCode();

        if (response != null)
        {
            string jsonString = await response.Content.ReadAsStringAsync();
            object json = JsonConvert.DeserializeObject<object>(jsonString);
            JObject joResponse = JObject.Parse(jsonString);
            JArray dataArray = (JArray)joResponse["data"]!;

            var airports = dataArray
                .Select((airport) =>
                {
                    return new AirportDto
                    {
                        AirportCode = airport["iataCode"].ToString(),
                        AirportName = airport["name"].ToString(),
                        CityName = airport["address"]?["cityName"].ToString(),
                        CountryName = airport["address"]?["countryName"].ToString(),
                        Latitude = airport["geoCode"]?["latitude"] != null ? double.Parse(airport["geoCode"]?["latitude"].ToString()!) : null,
                        Longitude = airport["geoCode"]?["longitude"] != null ? double.Parse(airport["geoCode"]?["longitude"].ToString()!) : null,
                        Distance = airport["distance"]?["value"] != null ? int.Parse(airport["distance"]?["value"].ToString()!) : null,
                        DistanceUnit = airport["distance"]?["unit"].ToString()
                    };
                })
                .ToList();

            return airports;
        }
        return new List<AirportDto>();
    }

    private List<TravellerDto> BuildTravelersList(int adults, int children, int seated_infants, int held_infants)
    {
        if (adults == 0)
        {
            throw new ArgumentException("There must be at least one adult");
        }
        if (adults < seated_infants + held_infants)
        {
            throw new ArgumentException("There must be no more infants then there are adults");
        }
        if (adults + children > 9)
        {
            throw new ArgumentException("There must be no more than 9 non-infant passengers");
        }
        int id = 1;
        List<TravellerDto> output = new List<TravellerDto>();
        for (int i = 0; i < adults; i++)
        {
            output.Add(new TravellerDto()
            {
                Id = id++,
                Type = "ADULT"
            });
        }
        for (int i = 0; i < children; i++)
        {
            output.Add(new TravellerDto()
            {
                Id = id++,
                Type = "CHILD"
            });
        }
        for (int i = 0; i < held_infants; i++)
        {
            output.Add(new TravellerDto()
            {
                Id = id++,
                Type = "HELD_INFANT",
                AssociatedAdultId = i+1
            });
        }
        for (int i = 0; i < seated_infants; i++)
        {
            output.Add(new TravellerDto()
            {
                Id = id++,
                Type = "SEATED_INFANT"
            });
        }
        return output;
    }

    private FlightDto FlightOfferToFlightDto(JToken item)
    {
        return new FlightDto
        {
            OriginAirportCode = (string?)item["itineraries"]?[0]?["segments"]?[0]?["departure"]?["iataCode"],
            DestinationAirportCode = (string?)item["itineraries"]?[0]?["segments"]?.Last()?["arrival"]?["iataCode"],
            DepartureTime = DateTime.Parse((string)item["itineraries"]?[0]?["segments"]?[0]?["departure"]?["at"]),
            ArrivalTime = DateTime.Parse((string)item["itineraries"]?[0]?["segments"]?.Last()?["arrival"]?["at"]),
            Duration = XmlConvert.ToTimeSpan(item["itineraries"]?[0]?["duration"].ToString()!),
            TotalPrice = item["price"]?["total"] != null ? decimal.Parse(item["price"]?["total"].ToString()!) : null,
            Currency = item["price"]?["currency"] != null ? item["price"]?["currency"].ToString() : null,
            Segments = item["itineraries"]?[0]?["segments"] != null ? ((JArray)item["itineraries"]?[0]?["segments"]!).Select((segment) =>
            {

                return new FlightSegmentDto
                {
                    OriginAirportCode = segment["departure"]?["iataCode"].ToString(),
                    DestinationAirportCode = segment["arrival"]?["iataCode"].ToString(),
                    DepartureTime = DateTime.Parse((string)segment["departure"]?["at"]),
                    ArrivalTime = DateTime.Parse((string)segment["arrival"]?["at"]),
                    Duration = XmlConvert.ToTimeSpan(segment["duration"].ToString()),
                    CarrierCode = segment["carrierCode"].ToString(),
                    FlightNumber = segment["number"].ToString(),
                };
            }).ToList() : new List<FlightSegmentDto>()
        };
    }

    private async Task UpdateAccessToken()
    {
        string url = "https://test.api.amadeus.com/v1/security/oauth2/token";
        var formData = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "client_id", apiId },
            { "client_secret", apiSecret }
        };

        using var body = new FormUrlEncodedContent(formData);

        System.Diagnostics.Debug.WriteLine($"{await body.ReadAsStringAsync()}\n");

        using HttpResponseMessage response = await PostAsync(url, body);
        
        response.EnsureSuccessStatusCode();

        if (response != null)
        {
            string jsonString = await response.Content.ReadAsStringAsync();
            object json = JsonConvert.DeserializeObject<object>(jsonString);
            JObject joResponse = JObject.Parse(jsonString);
            string? token = joResponse["access_token"].ToString();
            int secondsUntilExpiration = int.Parse(joResponse["expires_in"].ToString()); ;
            if (token != null)
            {
                apiAccessToken = token;
                tokenExpiration = DateTime.Now.AddSeconds(secondsUntilExpiration - 5);
            } else
            {
                throw new Exception("Failed to obtain Amadeus access token");
            }
        }
    }
}
