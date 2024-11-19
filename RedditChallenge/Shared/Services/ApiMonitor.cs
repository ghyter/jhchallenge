using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedditChallenge.Shared.Services;

public delegate Task<(int calledRequests,int remainingRequests, int resetTimeSeconds)> ApiMonitorDelegate(IServiceProvider serviceProvider);

public interface IApiMonitor
{
    Task StartAsync(ApiMonitorDelegate actionDelegate);
    Task StopAsync();
    LoopStats Status();

    DateTime? RunningSince { get; } // Expose start time

    event EventHandler? LoopStarted; // Event triggered when the loop starts
    event EventHandler? LoopStopped; // Event triggered when the loop stops
    event EventHandler<(TimeSpan Duration, int remainingRequests, int resetTimeSeconds, int loopCount,int usedRequests, TimeSpan Delay,DateTime MessageDate)>? LoopCalled; // Event triggered when the loop calls the action delegate
}

public record LoopStats(bool IsRunning, DateTime? RunningSince, int LoopCount = 0, int Remaining = 0, int Reset = 0, int Delay = 0);

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
    private TaskCompletionSource<bool>? _loopStartedTcs;
    private TaskCompletionSource<bool>? _loopStoppedTcs;

    private Task? _loopTask; // Track the monitoring loop task

    public event EventHandler? LoopStarted;
    public event EventHandler? LoopStopped;
    public event EventHandler<(TimeSpan Duration,int remainingRequests, int resetTimeSeconds, int loopCount,int usedRequests, TimeSpan Delay, DateTime MessageDate)>? LoopCalled;
    

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

    public ApiMonitor(ILogger<ApiMonitor> logger, IApiRateLimiter rateLimiter, IServiceProvider provider)
    {
        _logger = logger;
        _rateLimiter = rateLimiter;
        _cancellationTokenSource = new CancellationTokenSource();
        _isRunning = false;
        _serviceProvider = provider;
    }

    public async Task StartAsync(ApiMonitorDelegate actionDelegate)
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
            _isRunning = true;

            _loopStartedTcs = new TaskCompletionSource<bool>();
        }

        LoopStarted?.Invoke(this, EventArgs.Empty);

        _loopTask = Task.Factory.StartNew(() =>
            RunLoop(actionDelegate, _cancellationTokenSource.Token),
            _cancellationTokenSource.Token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

        await (_loopStartedTcs?.Task ?? Task.CompletedTask);
    }

    public async Task StopAsync()
    {
        _logger.LogInformation("ApiMonitor is stopping.");

        lock (_lock)
        {
            _loopStoppedTcs = new TaskCompletionSource<bool>();
        }

        _cancellationTokenSource.Cancel();

        await (_loopStoppedTcs?.Task ?? Task.CompletedTask);
    }

    public LoopStats Status()
    {
        lock (_lock)
        {
            return new LoopStats(_isRunning, _runningSince, _loopCount);
        }
    }

    private async Task RunLoop(ApiMonitorDelegate actionDelegate, CancellationToken cancellationToken)
    {
        try
        {
            _loopStartedTcs?.TrySetResult(true); // Signal that the loop has started

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _loopCount++;
                    
                    _logger.LogDebug("Loop iteration {loopCount}", _loopCount);

                    // Start tracking time
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                    var (calledRequests,remainingRequests, resetTimeSeconds) = await actionDelegate(_serviceProvider);
                    // Stop tracking time
                    stopwatch.Stop();
                    TimeSpan duration = TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds);

                    _rateLimiter.UpdateRateLimits(calledRequests,remainingRequests, resetTimeSeconds,duration);

                    LoopCalled?.Invoke(this, (duration,remainingRequests, resetTimeSeconds, _loopCount, calledRequests, _rateLimiter.CalculateEvenDelay(),DateTime.Now));

                    await _rateLimiter.ApplyEvenDelayAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred in the delegate function. Stopping the loop.");
                    _cancellationTokenSource.Cancel();
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
                _isRunning = false;
                _runningSince = null;
            }

            _loopStoppedTcs?.TrySetResult(true); // Signal that the loop has stopped

            _logger.LogInformation("RunLoop exiting.");
            LoopStopped?.Invoke(this, EventArgs.Empty);
        }
    }
}
