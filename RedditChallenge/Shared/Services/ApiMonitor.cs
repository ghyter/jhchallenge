using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using RedditChallenge.Shared.Repositories;


namespace RedditChallenge.Shared.Services;

public class ApiMonitor : IHostedService
{
    private readonly ILogger<ApiMonitor> _logger;
    private readonly Func<Task<(int remainingRequests, int resetTimeSeconds)>> _actionDelegate;
    private CancellationTokenSource _cancellationTokenSource;

    public ApiMonitor(
        ILogger<ApiMonitor> logger,
        Func<Task<(int remainingRequests, int resetTimeSeconds)>> actionDelegate)
    {
        _logger = logger;
        _actionDelegate = actionDelegate;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("ApiMonitor is starting.");
        _cancellationTokenSource = new CancellationTokenSource();
        Task.Run(() => RunLoop(_cancellationTokenSource.Token));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("ApiMonitor is stopping.");
        _cancellationTokenSource?.Cancel();
        return Task.CompletedTask;
    }

    private async Task RunLoop(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Call the delegate to perform API actions and fetch rate limiter parameters
                var (remainingRequests, resetTimeSeconds) = await _actionDelegate();

                // Calculate the delay based on rate limiter parameters
                int delay = remainingRequests > 0
                    ? (resetTimeSeconds * 1000) / remainingRequests
                    : resetTimeSeconds * 1000;

                delay = Math.Max(delay, 1000); // Ensure a minimum delay of 1 second

                // Wait for the calculated delay before the next iteration
                await Task.Delay(delay, cancellationToken);
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("RunLoop canceled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RunLoop.");
            throw;
        }
    }
}
