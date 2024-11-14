using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using RedditChallenge.Shared.Repositories;

namespace RedditChallenge.Shared.Services;

public interface IApiMonitor
{
    void SetActionDelegate(Func<Task<(int remainingRequests, int resetTimeSeconds)>> actionDelegate);
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);

}


public class ApiMonitor : IHostedService, IApiMonitor
{
    private readonly ILogger<ApiMonitor> _logger;
    private Func<Task<(int remainingRequests, int resetTimeSeconds)>> _actionDelegate;
    private readonly IApiRateLimiter _rateLimiter;
    private CancellationTokenSource _cancellationTokenSource;

    public ApiMonitor(
        ILogger<ApiMonitor> logger,
        IApiRateLimiter rateLimiter)
    {
        _logger = logger;
        _rateLimiter = rateLimiter;
    }


    public void SetActionDelegate(Func<Task<(int remainingRequests, int resetTimeSeconds)>> actionDelegate)
    {
        _actionDelegate = actionDelegate ?? throw new ArgumentNullException(nameof(actionDelegate));
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

                // Update the rate limiter with the latest parameters
                _rateLimiter.UpdateRateLimits(remainingRequests, resetTimeSeconds);

                // Apply the rate limiter delay
                await _rateLimiter.ApplyEvenDelayAsync();
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("RunLoop canceled by TaskCanceledException.");
        }
        finally
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("RunLoop canceled.");
            }
        }
    }
}
