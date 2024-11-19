using Microsoft.AspNetCore.SignalR;
using RedditChallenge.Shared.Services;
using RedditChallenge.Shared.Model;
using RedditChallenge.Shared.Repositories;

namespace RedditChallenge.Server.Hubs;

public class SubredditHub : Hub
{
    private readonly IApiMonitor _apiMonitor;
    private readonly IRedditStatsService _redditStatsService;
    private readonly BroadcastService _broadcastService;

     public SubredditHub(IApiMonitor apiMonitor, IRedditStatsService redditStatsService, BroadcastService broadcastService)
    {
        _apiMonitor = apiMonitor;
        _redditStatsService = redditStatsService;
        _broadcastService = broadcastService;

        // Subscribe to ApiMonitor events
        _apiMonitor.LoopStarted += async (sender, args) =>
            await PushMonitorEvent(new MonitorEventMessage("[START] Monitor started", DateTime.UtcNow));

        _apiMonitor.LoopStopped += async (sender, args) =>
            await PushMonitorEvent(new MonitorEventMessage("[STOP] Monitor stopped", DateTime.UtcNow));

        _apiMonitor.LoopCalled += async (sender, args) =>{
            await PushStats(new MonitorStatsMessage(true, _apiMonitor.RunningSince, args.loopCount,args.remainingRequests, args.resetTimeSeconds,args.usedRequests, args.Delay));
            };

        // Subscribe to RedditStatsService events
        _redditStatsService.StatsUpdated += async (sender, args) =>{
            await PushSubredditStatsEvent(new MonitorSubredditStatsMessage(args));
        };
    }

    // On client connection, send a welcome message
    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("ReceiveMonitorEvent",
            new MonitorEventMessage("Welcome to the Subreddit Monitor!", DateTime.UtcNow));
        await base.OnConnectedAsync();
    }

    // Push updated stats to all connected clients
    private async Task PushStats(MonitorStatsMessage stats)
    {
        await _broadcastService.PushStats(stats);
    }

    // Push monitor events to all connected clients
    private async Task PushMonitorEvent(MonitorEventMessage eventMessage)
    {
        await _broadcastService.PushMonitorEvent(eventMessage);
    }
    
    // Push monitor events to all connected clients
    private async Task PushSubredditStatsEvent(MonitorSubredditStatsMessage eventMessage)
    {
        await _broadcastService.PushSubredditStatsEvent(eventMessage);
    }


    // Command: Start the monitor loop for a specific subreddit
    public async Task StartSubredditLoop(string subreddit)
    {
        if (_apiMonitor.Status().IsRunning)
        {
            await _broadcastService.PushMonitorEvent(
                new MonitorEventMessage("[ERROR] The loop is already running.", DateTime.UtcNow));
            return;
        }

        await _apiMonitor.StartAsync(async sp =>
        {
            using var scope = sp.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<ISubredditRepository>();
            var stats = sp.GetService<IRedditStatsService>();

            var results = await repo.GetSubreddit(subreddit);

            stats?.UpdateSubredditStats(results!);

            return (
                results?.RateLimit.Used ?? 0,
                (int)Math.Floor(results?.RateLimit.Remaining ?? 0),
                results?.RateLimit.Reset ?? 0
            );
        });

        await _broadcastService.PushMonitorEvent(new MonitorEventMessage("[START] Loop started.", DateTime.UtcNow));
    }

    // Command: Stop the monitor loop
    public async Task StopSubredditLoop()
    {
        if (!_apiMonitor.Status().IsRunning)
        {
            await Clients.Caller.SendAsync("ReceiveMonitorEvent",
                new MonitorEventMessage("[ERROR] The loop is not running.", DateTime.UtcNow));
            return;
        }

        await _apiMonitor.StopAsync();

        await Clients.Caller.SendAsync("ReceiveMonitorEvent",
            new MonitorEventMessage("[STOP] Loop stopped.", DateTime.UtcNow));
    }


}
