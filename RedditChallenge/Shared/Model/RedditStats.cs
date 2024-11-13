using System;

namespace RedditChallenge.Shared.Model;
public class RedditStatist
{
    public SubRedditPost TopUpVotedPost { get; set; }
    public string TopAuthor { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.Now;
}
