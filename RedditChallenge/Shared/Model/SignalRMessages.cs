namespace RedditChallenge.Shared.Model;


public record MonitorStatsMessage(bool IsRunning, DateTime? RunningSince, int LoopCount, int Remaining, int Reset, int Used, TimeSpan Delay, TimeSpan Duration,DateTime MessageDate);

// Represents stats message sent to the client
public record MonitorApiStatsMessage(bool IsRunning, DateTime? RunningSince, int LoopCount, RateLimit RateLimit);

// Represents event message sent to the client
public record MonitorEventMessage(string Message, DateTime Timestamp);

public record MonitorSubredditStatsMessage(RedditStatistics stats);
