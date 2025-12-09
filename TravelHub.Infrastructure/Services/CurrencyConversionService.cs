using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravelHub.Domain.DTOs;
using TravelHub.Domain.Interfaces.Services;

namespace TravelHub.Infrastructure.Services;

public class CurrencyConversionService : AbstractThrottledApiService, ICurrencyConversionService
{
    public CurrencyConversionService(IHttpClientFactory httpClientFactory) : base(httpClientFactory, 1, TimeSpan.FromSeconds(1))
    {
    }

    public async Task<decimal> GetExchangeRate(string from, string to)
    {
        string url = $"https://hexarate.paikama.co/api/rates/latest/{from}";
        var param = new Dictionary<string, string?> { { "target", to } };
        string fullUrl = QueryHelpers.AddQueryString(url, param);

        using HttpResponseMessage response = await GetAsync(fullUrl);

        // Logowanie dla diagnostyki
        System.Diagnostics.Trace.TraceInformation($"Request URL: {fullUrl}");
        System.Diagnostics.Trace.TraceInformation($"Response Status: {response.StatusCode}");

        response.EnsureSuccessStatusCode();

        string jsonString = await response.Content.ReadAsStringAsync();
        System.Diagnostics.Trace.TraceInformation($"Response JSON: {jsonString}");

        try
        {
            // Uproszczona deserializacja
            var jsonObject = JObject.Parse(jsonString);

            if (jsonObject["data"] == null || jsonObject["data"]!["mid"] == null)
            {
                System.Diagnostics.Trace.TraceError($"Unexpected JSON structure: {jsonString}");
                throw new InvalidOperationException("Invalid response format from API");
            }

            string midValue = jsonObject["data"]!["mid"]!.ToString();

            if (!decimal.TryParse(midValue, out decimal rate))
            {
                System.Diagnostics.Trace.TraceError($"Cannot parse mid value: {midValue}");
                throw new FormatException($"Invalid rate value: {midValue}");
            }

            return rate;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError($"JSON parsing error: {ex.Message}\nJSON: {jsonString}");
            throw new HttpRequestException($"Failed to parse API response: {ex.Message}", ex);
        }
    }
}
