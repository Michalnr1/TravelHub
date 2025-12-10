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

    private async Task<decimal> GetRateToPLN(string from)
    {
        if (from.ToLower() == "pln") return 1;
        string url = $"https://api.nbp.pl/api/exchangerates/rates/a/{from}";
        using HttpResponseMessage response = await GetAsync(url);
        response.EnsureSuccessStatusCode();
        string jsonString = await response.Content.ReadAsStringAsync();
        try
        {
            // Uproszczona deserializacja
            var jsonObject = JObject.Parse(jsonString);

            if (jsonObject["rates"] == null || jsonObject["rates"]![0] == null || jsonObject["rates"]![0]!["mid"] == null)
            {
                System.Diagnostics.Trace.TraceError($"Unexpected JSON structure: {jsonString}");
                throw new InvalidOperationException("Invalid response format from API");
            }

            string midValue = jsonObject["rates"]![0]!["mid"]!.ToString();

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

    public async Task<decimal> GetExchangeRate(string from, string to)
    {
        decimal fromRate = await GetRateToPLN(from);
        decimal toRate = await GetRateToPLN(to);
        return Math.Round(fromRate / toRate, 4);
    }
}
