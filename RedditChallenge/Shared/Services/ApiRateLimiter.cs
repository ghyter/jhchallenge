using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace RedditChallenge.Shared.Services;
 
public interface IApiRateLimiter
{
    public void UpdateRateLimits(int remainingRequests, int resetTimeSeconds);
    public int CalculateEvenDelay();
    public Task ApplyEvenDelayAsync();
}

public class ApiRateLimiter:IApiRateLimiter
{
    private int _remainingRequests; // x-ratelimit-remaining
    private int _resetTimeSeconds; // x-ratelimit-reset (in seconds)
    private DateTime _lastUpdated; // Tracks the time when headers were last parsed

    public ApiRateLimiter()
    {
        _remainingRequests = 100; // Default to max limit
        _resetTimeSeconds = 60;   // Default reset period (1 minute)
        _lastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the rate limit values from the Reddit API headers.
    /// </summary>
    /// <param name="response">The HTTP response containing rate-limit headers.</param>
    public void UpdateRateLimits(int remainingRequests, int resetTimeSeconds)
    {
        _remainingRequests = remainingRequests;
        _resetTimeSeconds = resetTimeSeconds;
        _lastUpdated = DateTime.UtcNow;

        Console.WriteLine($"Rate Limits Updated: Remaining={_remainingRequests}, Reset={_resetTimeSeconds}s");
    }

    /// <summary>
    /// Calculates the even delay between requests.
    /// </summary>
    /// <returns>Delay in milliseconds.</returns>
    public int CalculateEvenDelay()
    {
        // If no time remains, or reset has already passed, return no delay
        if (_resetTimeSeconds <= 0)
        {
            return 0;
        }

        // If no requests remain, wait for the full reset time
        if (_remainingRequests <= 0)
        {
            return _resetTimeSeconds * 1000; // Return delay in milliseconds
        }

        // Spread remaining requests evenly over the remaining time
        return (_resetTimeSeconds * 1000) / _remainingRequests; // Delay in milliseconds
    }


    /// <summary>
    /// Applies an even delay before the next request.
    /// </summary>
    public async Task ApplyEvenDelayAsync()
    {
        int delay = CalculateEvenDelay();
        Console.WriteLine($"Delaying next request by {delay}ms...");
        await Task.Delay(delay);
    }
}
