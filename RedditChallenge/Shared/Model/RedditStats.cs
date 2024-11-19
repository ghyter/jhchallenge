using System;

namespace RedditChallenge.Shared.Model;
public class RedditStatistics
{
    public SubRedditPost? TopUpVotedPost { get; set; }
    public string? TopAuthorByPosts { get; set; }
    public int TopAuthorPostCount { get; set; } = 0;
    public DateTime LastUpdated { get; set; } = DateTime.Now;

    public string? TopAuthorByUpVotes {get;set;}
    public int TopAuthorByUpVotesCount {get;set;} = 0;

    public DateTime? EarliestPost {get;set;}
    public DateTime? LatestPost {get;set;}


}
