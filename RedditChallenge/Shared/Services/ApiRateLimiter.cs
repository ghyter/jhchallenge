using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RedditChallenge.Shared.Services;

public interface IApiRateLimiter
{
    void UpdateRateLimits(int usedRequests, int remainingRequests, int resetTimeSeconds,TimeSpan Duration);
    TimeSpan CalculateEvenDelay();
    Task ApplyEvenDelayAsync(CancellationToken cancellationToken);

    ApiRateLimiterValues Values();
}

public record ApiRateLimiterValues (int UsedRequests, int RemaininRequests, int ResetTimeSeconds, DateTime LastUpdated, TimeSpan Duration);

public class ApiRateLimiter : IApiRateLimiter
{
    private readonly ILogger<ApiRateLimiter> _logger;

    private int _usedRequests;
    private int _remainingRequests; // x-ratelimit-remaining
    private int _resetTimeSeconds; // x-ratelimit-reset (in seconds)
    private DateTime _lastUpdated; // Tracks the time when headers were last parsed
    private TimeSpan _duration;

    public ApiRateLimiter(ILogger<ApiRateLimiter> logger)
    {
        _logger = logger;
    
        
        _usedRequests = 0;
        _remainingRequests = 100; // Default to max limit
        _resetTimeSeconds = 60;   // Default reset period (1 minute)
        _lastUpdated = DateTime.UtcNow;
        _duration = TimeSpan.Zero;
    }

    public ApiRateLimiterValues Values() => new ApiRateLimiterValues(_usedRequests,_remainingRequests,_resetTimeSeconds,_lastUpdated,_duration);

    /// <summary>
    /// Updates the rate limit values from the Reddit API headers.
    /// </summary>
    public void UpdateRateLimits(int usedRequests, int remainingRequests, int resetTimeSeconds, TimeSpan Duration)
    {
        if (usedRequests < 0) throw new ArgumentOutOfRangeException(nameof(usedRequests), "Used requests cannot be negative.");
        if (remainingRequests < 0) remainingRequests = 0;
        if (resetTimeSeconds < 0) resetTimeSeconds = 0;

        _usedRequests = usedRequests;
        _remainingRequests = remainingRequests;
        _resetTimeSeconds = resetTimeSeconds;
        _lastUpdated = DateTime.UtcNow;
        _duration = Duration;

        _logger.LogInformation("Rate Limits Updated: Used={UsedRequests}, Remaining={RemainingRequests}, Reset={ResetTimeSeconds}s, LastUpdated={LastUpdated}, Duration= {duration}",
            _usedRequests, _remainingRequests, _resetTimeSeconds, _lastUpdated, _duration);
    }


    /// <summary>
    /// Calculates the even delay between requests.
    /// </summary>
    public TimeSpan CalculateEvenDelay()
{
    // Subtract the duration of the last request from the reset time
    int adjustedResetTime = Math.Max(_resetTimeSeconds - (int)_duration.TotalSeconds, 0);

    // If no time remains or no remaining requests, return a full reset delay
    if (adjustedResetTime <= 0 || _remainingRequests <= 0)
    {
        _logger.LogWarning("No remaining requests or reset time passed. Applying full reset delay.");
        return TimeSpan.FromMilliseconds(_resetTimeSeconds * 1000); // Delay in milliseconds
    }

    // Spread remaining requests evenly over the adjusted reset time
    int delay = (adjustedResetTime * 1000) / _remainingRequests; // Delay in milliseconds
    _logger.LogDebug("Calculated even delay: {Delay}ms (using adjusted reset time: {AdjustedResetTime}s and remaining requests: {RemainingRequests})",
        delay, adjustedResetTime, _remainingRequests);

    return TimeSpan.FromMilliseconds(delay);
}


    /// <summary>
    /// Applies an even delay before the next request.
    /// </summary>
    public async Task ApplyEvenDelayAsync(CancellationToken cancellationToken)
    {
        TimeSpan delay = CalculateEvenDelay();
        _logger.LogInformation("Delaying next request by {Delay}ms...", delay);
        if (delay > TimeSpan.Zero)
        {
            await Task.Delay(delay, cancellationToken);
        }
    }
}
