using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedditChallenge.Shared.Services;

public interface IApiMonitor
{
    Task StartAsync(Func<Task<(int remainingRequests, int resetTimeSeconds)>> actionDelegate);
    Task StopAsync();
    bool Status();
    DateTime? RunningSince { get; } // Expose start time
}

public class ApiMonitor : IApiMonitor
{
    private readonly ILogger<ApiMonitor> _logger;
    private readonly IApiRateLimiter _rateLimiter;
    private CancellationTokenSource _cancellationTokenSource;
    private readonly object _lock = new();
    private DateTime? _runningSince;
    private bool _isRunning; // Track whether the loop is actively running

    public DateTime? RunningSince
    {
        get
        {
            lock (_lock)
            {
                return _runningSince;
            }
        }
    }

    public ApiMonitor(ILogger<ApiMonitor> logger, IApiRateLimiter rateLimiter)
    {
        _logger = logger;
        _rateLimiter = rateLimiter;
        _cancellationTokenSource = new CancellationTokenSource();
        _isRunning = false;
    }

    public Task StartAsync(Func<Task<(int remainingRequests, int resetTimeSeconds)>> actionDelegate)
    {
        if (actionDelegate is null)
        {
            throw new ArgumentNullException(nameof(actionDelegate), "Action delegate cannot be null.");
        }

        _logger.LogInformation("ApiMonitor is starting.");

        lock (_lock)
        {
            if (_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = new CancellationTokenSource();
            }

            _runningSince = DateTime.UtcNow;
            _isRunning = true; // Set to true when the loop starts
        }

        Task.Run(() => RunLoop(actionDelegate, _cancellationTokenSource.Token));
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        _logger.LogInformation("ApiMonitor is stopping.");

        lock (_lock)
        {
            _cancellationTokenSource.Cancel();
            _isRunning = false; // Set to false when the loop stops
            _runningSince = null;
        }

        return Task.CompletedTask;
    }

    public bool Status()
    {
        lock (_lock)
        {
            return _isRunning;
        }
    }

    private async Task RunLoop(Func<Task<(int remainingRequests, int resetTimeSeconds)>> actionDelegate, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Perform the delegate action and update rate limiter
                    var (remainingRequests, resetTimeSeconds) = await actionDelegate();
                    _rateLimiter.UpdateRateLimits(remainingRequests, resetTimeSeconds);

                    // Apply delay
                    await _rateLimiter.ApplyEvenDelayAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred in the delegate function. Stopping the loop.");
                    await StopAsync();
                    throw;
                }
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("RunLoop canceled by TaskCanceledException.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred in the RunLoop.");
        }
        finally
        {
            lock (_lock)
            {
                _isRunning = false; // Ensure it's set to false if the loop exits unexpectedly
            }

            _logger.LogInformation("RunLoop exiting.");
        }
    }
}
