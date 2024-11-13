using System;
using RedditChallenge.Shared.Repositories;

namespace RedditChallenge.Shared.Services;
public class RedditStatsService {
/*
    private readonly ApiRateLimiter _rateLimiter;
    private readonly ISubredditRepository _subredditRepository;

    private bool _continue = true;

    public RedditStatsService(IApiRateLimiter rateLimiter, ISubredditRepository subredditRepository)
    {
        _rateLimiter = rateLimiter;
        _subredditRepository = subredditRepository;
    }

    public async Task GetSubredditStats(string subreddit)
    {
        var subredditRepository = new SubredditRepository(new HttpClient(), new RedditAuthRepository());
        var subredditResponse = await subredditRepository.GetSubreddit(subreddit);

        if (subredditResponse != null)
        {
            Console.WriteLine($"Subreddit: {subredditResponse.Data.DisplayName}");
            Console.WriteLine($"Subscribers: {subredditResponse.Data.Subscribers}");
            Console.WriteLine($"Active Users: {subredditResponse.Data.ActiveUsers}");
            Console.WriteLine($"Accounts Active: {subredditResponse.Data.AccountsActive}");
            Console.WriteLine($"Rate Limit: {subredditResponse.RateLimit.Remaining}/{subredditResponse.RateLimit.Used} requests remaining");
        }
    }

    

    public async Task PostLoop(string subreddit)
    {
        while (_continue)
        {
            var subreddit = "csharp";
            await GetSubredditStats(subreddit);
            await await _rateLimiter.CalculateEvenDelay();
        }
    }


*/

}