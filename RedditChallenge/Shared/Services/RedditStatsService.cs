using System;
using Microsoft.Extensions.Logging;
using RedditChallenge.Shared.Model;
using RedditChallenge.Shared.Repositories;
using System.Collections.Generic;
using System.Linq;

namespace RedditChallenge.Shared.Services;

public interface IRedditStatsService
{
    void UpdateSubredditStats(RedditRootResponse<SubredditResponse> response);
    RedditStatistics? GetLatestStats();
    event EventHandler<RedditStatistics>? StatsUpdated;
}

public class RedditStatsService : IRedditStatsService
{
    private readonly ILogger<RedditStatsService> _logger;
    private RedditStatistics? _stats;
    private readonly object _statsLock = new();

    public event EventHandler<RedditStatistics>? StatsUpdated;

    public RedditStatsService(ILogger<RedditStatsService> logger)
    {
        _logger = logger;
        _logger.LogDebug("RedditStatsService instantiated");
    }

    public void UpdateSubredditStats(RedditRootResponse<SubredditResponse>? response)
    {
        _logger.LogDebug("UpdateSubredditStats method called");
        
        if (response?.Data?.Children == null)
        {
            _logger.LogWarning("Response data is null or missing children");
            return;
        }

        RedditStatistics stat = new();
        var authors = new Dictionary<string, int>();
        var authorsUpVotes = new Dictionary<string, int>();
        DateTime? earliestPost = null;
        DateTime? latestPost = null;

        foreach (var post in response.Data.Children)
        {
            if (post.Data == null)
            {
                _logger.LogWarning("Post data is null, skipping this post");
                continue;
            }

            var data = post.Data;
            var author = data.Author ?? string.Empty;

            if (string.IsNullOrEmpty(author))
            {
                _logger.LogDebug("Post with no valid author information, skipping");
                continue;
            }

            // Track author's post count
            if (!authors.ContainsKey(author))
            {
                _logger.LogDebug($"New author found: {author}");
                authors[author] = 0;
            }
            authors[author]++;
            _logger.LogDebug($"Incremented post count for author: {author}, Total Posts: {authors[author]}");

            // Track author's upvote count
            if (!authorsUpVotes.ContainsKey(author))
            {
                authorsUpVotes[author] = 0;
            }
            authorsUpVotes[author] += data.Ups ?? 0;

            // Update the top upvoted post
            if (stat.TopUpVotedPost == null || data.Ups > stat.TopUpVotedPost.Ups)
            {
                _logger.LogDebug($"New top upvoted post found: {data.Title} with {data.Ups} upvotes");
                stat.TopUpVotedPost = data;
            }

            // Track the earliest and latest post times
            DateTime postTime = data.CreatedUtcDateTime;
            if (earliestPost == null || postTime < earliestPost)
            {
                earliestPost = postTime;
            }

            if (latestPost == null || postTime > latestPost)
            {
                latestPost = postTime;
            }
        }

        // Determine the author with the most posts
        if (authors.Count > 0)
        {
            stat.TopAuthorByPosts = authors.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;
            stat.TopAuthorPostCount = authors[stat.TopAuthorByPosts];
            _logger.LogDebug($"Top author determined: {stat.TopAuthorByPosts} with {authors[stat.TopAuthorByPosts]} posts");
        }

        // Determine the author with the most upvotes
        if (authorsUpVotes.Count > 0)
        {
            stat.TopAuthorByUpVotes = authorsUpVotes.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;
            stat.TopAuthorByUpVotesCount = authorsUpVotes[stat.TopAuthorByUpVotes];
            _logger.LogDebug($"Top author by upvotes determined: {stat.TopAuthorByUpVotes} with {authorsUpVotes[stat.TopAuthorByUpVotes]} upvotes");
        }

        // Set the earliest and latest post times
        stat.EarliestPost = earliestPost;
        stat.LatestPost = latestPost;

        // Update the stats with thread safety
        lock (_statsLock)
        {
            _logger.LogDebug("Acquired lock to update stats");
            _stats = stat;
        }

        // Call the event outside the lock to minimize lock contention
        _logger.LogDebug("Triggering StatsUpdated event");
        OnStatsUpdated(stat);

        // Log the updated statistics
        _logger.LogInformation("Updated Stats");
        _logger.LogInformation($"Top Author by Posts: {stat.TopAuthorByPosts}");
        _logger.LogInformation($"Top Post: {stat.TopUpVotedPost?.Title}");
        _logger.LogInformation($"Top Author by Upvotes: {stat.TopAuthorByUpVotes}");
        _logger.LogInformation($"Earliest Post: {stat.EarliestPost}");
        _logger.LogInformation($"Latest Post: {stat.LatestPost}");
        _logger.LogInformation($"Processed {response.Data.Children.Count} posts.");
    }

    protected virtual void OnStatsUpdated(RedditStatistics stat)
    {
        _logger.LogDebug("Raising StatsUpdated event");
        StatsUpdated?.Invoke(this, stat);
    }

    public RedditStatistics? GetLatestStats()
    {
        _logger.LogDebug("GetLatestStats called");
        return _stats;
    }
}
