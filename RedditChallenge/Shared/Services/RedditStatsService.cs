using System;
using Microsoft.Extensions.Logging;
using RedditChallenge.Shared.Model;
using RedditChallenge.Shared.Repositories;

namespace RedditChallenge.Shared.Services;

public interface IRedditStatsService
{
    // Method to update subreddit statistics using the given response data
    void UpdateSubredditStats(RedditRootResponse<SubredditResponse> response);
    
    // Method to get the latest subreddit statistics
    RedditStatistics? GetLatestStats();
    
    // Event that is triggered when the statistics are updated
    event EventHandler<RedditStatistics>? StatsUpdated;
}

public class RedditStatsService : IRedditStatsService
{
    // Logger instance to log information
    private readonly ILogger<RedditStatsService> _logger;
    
    // Stores the current statistics
    private RedditStatistics? _stats;
    
    // Lock object to ensure thread safety when updating stats
    private readonly object _statsLock = new();

    // Event that is triggered when the statistics are updated
    public event EventHandler<RedditStatistics>? StatsUpdated;

    // Constructor to initialize the logger
    public RedditStatsService(ILogger<RedditStatsService> logger)
    {
        _logger = logger;
        _logger.LogDebug("RedditStatsService instantiated");
    }

    // Method to update subreddit statistics based on the response data
    public void UpdateSubredditStats(RedditRootResponse<SubredditResponse>? response)
    {
        _logger.LogDebug("UpdateSubredditStats method called");
        
        // Return early if response or required data is null
        if (response?.Data?.Children == null)
        {
            _logger.LogWarning("Response data is null or missing children");
            return;
        }

        RedditStatistics stat = new(); // Initialize a new RedditStatistics object
        var authors = new Dictionary<string, int>(); // Dictionary to keep track of post authors and their post counts

        // Iterate through each post in the response data
        foreach (var post in response.Data.Children)
        {
            // Skip this iteration if the post data is null
            if (post.Data == null)
            {
                _logger.LogWarning("Post data is null, skipping this post");
                continue;
            }

            var data = post.Data;
            var author = data.Author ?? string.Empty; // Get the author, default to an empty string if null
            
            // Skip posts with no valid author information
            if (string.IsNullOrEmpty(author))
            {
                _logger.LogDebug("Post with no valid author information, skipping");
                continue;
            }

            // Increment the author's post count or add the author if not already in the dictionary
            if (!authors.ContainsKey(author))
            {
                _logger.LogDebug($"New author found: {author}");
                authors[author] = 0;
            }
            authors[author]++;
            _logger.LogDebug($"Incremented post count for author: {author}, Total Posts: {authors[author]}");

            // Update the top upvoted post if this post has more upvotes than the current top post
            if (stat.TopUpVotedPost == null || data.Ups > stat.TopUpVotedPost.Ups)
            {
                _logger.LogDebug($"New top upvoted post found: {data.Title} with {data.Ups} upvotes");
                stat.TopUpVotedPost = data;
            }
        }

        // Determine the author with the most posts and set it in the statistics
        if (authors.Count > 0)
        {
            stat.TopAuthor = authors.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;
            _logger.LogDebug($"Top author determined: {stat.TopAuthor} with {authors[stat.TopAuthor]} posts");
        }

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
        _logger.LogInformation($"Top Author: {stat.TopAuthor}");
        _logger.LogInformation($"Top Post: {stat.TopUpVotedPost?.Title}");
        _logger.LogInformation($"Processed {response.Data.Children.Count} posts.");
    }

    // Method to trigger the StatsUpdated event
    protected virtual void OnStatsUpdated(RedditStatistics stat)
    {
        // Log before raising the event
        _logger.LogDebug("Raising StatsUpdated event");
        StatsUpdated?.Invoke(this, stat);
    }

    // Method to get the latest statistics
    public RedditStatistics? GetLatestStats()
    {
        _logger.LogDebug("GetLatestStats called");
        return _stats;
    }
}
