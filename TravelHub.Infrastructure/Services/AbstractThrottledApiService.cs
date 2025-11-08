using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelHub.Infrastructure.Services;

public class AbstractThrottledApiService
{
    private static readonly SemaphoreSlim _lock = new(1, 1);
    private static readonly Queue<DateTime> _requestTimestamps = new();

    //private readonly int _maxRequests;
    private readonly TimeSpan _timeWindow;

    protected readonly HttpClient _httpClient;

    protected AbstractThrottledApiService(
        IHttpClientFactory httpClientFactory,
        int maxRequestsPerWindow,
        TimeSpan timeWindow)
    {
        _httpClient = httpClientFactory.CreateClient();
        //_maxRequests = maxRequestsPerWindow;
        _timeWindow = timeWindow;
        for (int i = 0; i < maxRequestsPerWindow; i++)
        {
            _requestTimestamps.Enqueue(DateTime.MinValue);
        }
    }

    //change to HttpResponseMessage
    protected async Task<HttpResponseMessage> ExecuteThrottledAsync(Func<Task<HttpResponseMessage>> httpOperation)
    {
        

        var earliest = DateTime.MaxValue;
        var foundEmpty = true;
        while (foundEmpty)
        {
            await _lock.WaitAsync();
            try
            {
                if (_requestTimestamps.Count > 0)
                {
                    earliest = _requestTimestamps.Dequeue();
                    foundEmpty = false;
                }
            } finally
            {
                _lock.Release();
            }         
            if (foundEmpty) await Task.Delay(100);
        }
        var now = DateTime.UtcNow;
        var waitTime = _timeWindow - (now - earliest);
        if (waitTime > TimeSpan.Zero)
        {
            await Task.Delay(waitTime);
        }

        // Record the new request timestamp
        await _lock.WaitAsync();
        try
        {
            _requestTimestamps.Enqueue(DateTime.UtcNow);
        }
        finally
        {
            _lock.Release();
        }

        // Execute the actual HTTP operation
        return await httpOperation();
    }

    protected Task<HttpResponseMessage> GetAsync(string url)
        => ExecuteThrottledAsync(() => _httpClient.GetAsync(url));

    protected Task<HttpResponseMessage> PostAsync(string url, HttpContent body)
        => ExecuteThrottledAsync(() => _httpClient.PostAsync(url, body));

    protected Task<HttpResponseMessage> GetWithHeadersAsync(string url,
                                                            Dictionary<string, string>? headers = null)
    {
        return ExecuteThrottledAsync(async () =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);

            if (headers != null)
            {
                foreach (var (key, value) in headers)
                {
                    request.Headers.Add(key, value);
                }
            }

            return await _httpClient.SendAsync(request);
        });
    }

    protected Task<HttpResponseMessage> PostWithHeadersAsync(string url,
                                                            HttpContent content,
                                                            Dictionary<string, string>? headers = null)
    {
        return ExecuteThrottledAsync(async () =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };

            if (headers != null)
            {
                foreach (var (key, value) in headers)
                {
                    request.Headers.Add(key, value);
                }
            }

            return await _httpClient.SendAsync(request);
        });
    }
}
