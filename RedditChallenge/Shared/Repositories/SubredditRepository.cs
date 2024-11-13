using System.Net.Http.Headers;
using System.Reflection.Metadata;
using System.Text.Json;
using RedditChallenge.Shared.Model;

namespace RedditChallenge.Shared.Repositories;


public interface ISubredditRepository
{
    Task<RedditRootResponse<SubredditResponse>?> GetSubreddit(string subreddit);
}

public static class StatTypes {
    public const string TopAllTimePost = "top.json?t=all&limit=1";
}



public class SubredditRepository:ISubredditRepository
{
    private readonly HttpClient _client;
    private readonly IRedditAuthRepository _authRepository;

    public SubredditRepository(HttpClient client, IRedditAuthRepository authRepository)
    {
        _client = client;
        _authRepository = authRepository;
    }

/// <summary>
/// Get a subreddit by name
/// </summary>
/// <param name="subreddit"></param>
/// <returns></returns>
/// <exception cref="Exception"></exception>
    public async Task<RedditRootResponse<SubredditResponse>?> GetSubreddit(string subreddit)
    {
        var token = await _authRepository.GetAuthToken();
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://oath.reddit.com/r/{subreddit}/new.json");
        
        request.Headers.UserAgent.ParseAdd("RedditChallenge/1.0 (by /u/Dependent-Bar-8662)");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",token);

        var response = await _client.SendAsync(request);

        RateLimit rateLimit = new RateLimit();
        rateLimit.Used = int.Parse(response.Headers.GetValues("X-Ratelimit-Used").First());
        rateLimit.Remaining = decimal.Parse(response.Headers.GetValues("X-Ratelimit-Remaining").First());
        rateLimit.Reset = int.Parse(response.Headers.GetValues("X-Ratelimit-Reset").First());

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to get subreddits: {response.ReasonPhrase}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var subreddits = JsonSerializer.Deserialize<RedditRootResponse<SubredditResponse>>(json);
        if (subreddits != null)
        {
            subreddits.RateLimit = rateLimit;
        }
        
        return subreddits;
    }
}
