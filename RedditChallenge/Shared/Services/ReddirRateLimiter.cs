using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace RedditChallenge.Shared.Services;

public class RedditRateLimiter
{
    private int _remainingRequests; // x-ratelimit-remaining
    private int _resetTimeSeconds; // x-ratelimit-reset (in seconds)
    private DateTime _lastUpdated; // Tracks the time when headers were last parsed

    public RedditRateLimiter()
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
        // Time remaining until reset
        int timeElapsed = (int)(DateTime.UtcNow - _lastUpdated).TotalSeconds;
        int timeRemaining = _resetTimeSeconds - timeElapsed; // Ensure positive value
        if (timeRemaining <= 0) return 0; // Reset time has passed

        // Calculate even delay (time remaining divided by remaining requests)
        return _remainingRequests > 0
            ? (timeRemaining * 1000) / _remainingRequests // Delay in milliseconds
            : timeRemaining * 1000; // If no remaining requests, wait until reset
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
