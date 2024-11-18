using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedditChallenge.Shared.Services;

public delegate Task<(int remainingRequests, int resetTimeSeconds)> ApiMonitorDelegate(IServiceProvider serviceProvider);

public interface IApiMonitor
{
    Task StartAsync(ApiMonitorDelegate actionDelegate);
    Task StopAsync();
    LoopStats Status();

    DateTime? RunningSince { get; } // Expose start time

    event EventHandler? LoopStarted; // Event triggered when the loop starts
    event EventHandler? LoopStopped; // Event triggered when the loop stops
    event EventHandler<(int remainingRequests, int resetTimeSeconds, int loopCount)>? LoopCalled; // Event triggered when the loop calls the action delegate
}

public record LoopStats(bool IsRunning, DateTime? RunningSince, int LoopCount = 0);

public class ApiMonitor : IApiMonitor
{
    private IServiceProvider _serviceProvider;
    private readonly ILogger<ApiMonitor> _logger;
    private readonly IApiRateLimiter _rateLimiter;
    private CancellationTokenSource _cancellationTokenSource;
    private readonly object _lock = new(); // Lock object for thread safety
    private DateTime? _runningSince;
    private int _loopCount = 0;
    private bool _isRunning; // Track whether the loop is actively running

    // Track the monitoring loop task
    private Task? _loopTask;

    // Events to track loop lifecycle
    public event EventHandler? LoopStarted;
    public event EventHandler? LoopStopped;
    public event EventHandler<(int remainingRequests, int resetTimeSeconds, int loopCount)>? LoopCalled;

    // Property to get the time when the loop started
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

    // Constructor to initialize dependencies
    public ApiMonitor(ILogger<ApiMonitor> logger, IApiRateLimiter rateLimiter, IServiceProvider provider)
    {
        _logger = logger;
        _rateLimiter = rateLimiter;
        _cancellationTokenSource = new CancellationTokenSource();
        _isRunning = false;
        _serviceProvider = provider;
    }

    // Method to start the monitoring loop
    public Task StartAsync(ApiMonitorDelegate actionDelegate)
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
            _loopCount = 0;
            _isRunning = true; // Set to true when the loop starts
        }

        LoopStarted?.Invoke(this, EventArgs.Empty); // Trigger LoopStarted event

        // Start the loop and track the task
        _loopTask = Task.Factory.StartNew(() =>
            RunLoop(actionDelegate, _cancellationTokenSource.Token),
            _cancellationTokenSource.Token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

        return Task.CompletedTask;
    }

    // Method to stop the monitoring loop
    public Task StopAsync()
    {
        _logger.LogInformation("ApiMonitor is stopping.");
        _cancellationTokenSource.Cancel(); // Signal cancellation to the loop
        return Task.CompletedTask;
    }

    // Method to get the current status of the monitoring loop
    public LoopStats Status()
    {
        lock (_lock)
        {
            return new LoopStats(_isRunning, _runningSince, _loopCount);
        }
    }

    // Private method to run the monitoring loop
    private async Task RunLoop(ApiMonitorDelegate actionDelegate, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _loopCount++; // Increment loop count for each iteration
                    _logger.LogDebug("Loop iteration {loopCount}", _loopCount);

                    // Perform the delegate action and update rate limiter
                    var (remainingRequests, resetTimeSeconds) = await actionDelegate(_serviceProvider);
                    _rateLimiter.UpdateRateLimits(remainingRequests, resetTimeSeconds);

                    // Trigger event after each delegate call
                    LoopCalled?.Invoke(this, (remainingRequests, resetTimeSeconds, _loopCount));

                    _logger.LogDebug($"Remaining Requests: {remainingRequests}, Reset Time (seconds): {resetTimeSeconds}");

                    // Apply delay based on rate limits
                    await _rateLimiter.ApplyEvenDelayAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred in the delegate function. Stopping the loop.");
                    _cancellationTokenSource.Cancel(); // Stop the loop if an error occurs
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
                _isRunning = false; // Ensure it's set to false if the loop exits
                _runningSince = null;
            }

            _logger.LogInformation("RunLoop exiting.");
            LoopStopped?.Invoke(this, EventArgs.Empty); // Trigger LoopStopped event after the loop exits
        }
    }
}
