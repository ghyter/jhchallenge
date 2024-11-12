using System.Net.Http.Headers;
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

    public async Task<RedditRootResponse<SubredditResponse>?> GetSubreddit(string subreddit)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/r/{subreddit}/top.json");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _authRepository.GetAuthToken());

        var response = await _client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to get subreddits: {response.ReasonPhrase}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var subreddits = JsonSerializer.Deserialize<RedditRootResponse<SubredditResponse>>(json);

        return subreddits;
    }
}
