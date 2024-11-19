using Microsoft.AspNetCore.SignalR;
using RedditChallenge.Shared.Model;

namespace RedditChallenge.Server.Hubs;

public class BroadcastService
{
    private readonly IHubContext<SubredditHub> _hubContext;

    public BroadcastService(IHubContext<SubredditHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task PushStats(MonitorStatsMessage stats)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveMonitorStats", stats);
    }

    public async Task PushSubredditStatsEvent(MonitorSubredditStatsMessage eventMessage)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveSubredditStats", eventMessage);
    }

    public async Task PushMonitorEvent(MonitorEventMessage eventMessage)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveMonitorEvent", eventMessage);
    }
}
