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
        Dictionary<string, string?> param = new Dictionary<string, string?>() { { "target", to } };
        Uri newUrl = new Uri(QueryHelpers.AddQueryString(url, param));

        using HttpResponseMessage response = await GetAsync(newUrl.ToString());
        System.Diagnostics.Debug.WriteLine($"{await response.Content.ReadAsStringAsync()}\n");
        response.EnsureSuccessStatusCode();

        try
        {
            string jsonString = await response.Content.ReadAsStringAsync();
            object json = JsonConvert.DeserializeObject<object>(jsonString);
            JObject joResponse = JObject.Parse(jsonString);
            JObject data = (JObject)joResponse["data"]!;

            return decimal.Parse(data["mid"].ToString());
        } catch
        {
            throw new HttpRequestException();
        }
    }
}
