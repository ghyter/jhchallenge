using System.Net.Http.Headers;
using System.Text;
using RedditChallenge.Shared.Model;


namespace RedditChallenge.Shared.Repositories;
public record RedditOATHSettings(string ClientId, string ClientSecret);

public interface IRedditAuthRepository
{
    Task<string> GetAuthToken();
    RedditOATHSettings GetConfig();

}


public class RedditAuthRepository: IRedditAuthRepository
{
   private readonly HttpClient _client = new HttpClient();

    private RedditOATHSettings _config;

    private RedditAuthToken? _authToken;

    public RedditAuthRepository(HttpClient client)
    {
        _client = client;
        _config = new RedditOATHSettings(Environment.GetEnvironmentVariable("REDDIT_CLIENT_ID")?? string.Empty,
            Environment.GetEnvironmentVariable("REDDIT_CLIENT_SECRET")?? string.Empty);
    }

    public RedditAuthRepository(HttpClient client, RedditOATHSettings settings)
    {
        _client = client;
        _config = settings;
    }

    public RedditOATHSettings GetConfig()
    {
        return _config;
    }


/// <summary>
///   Get the current auth tokent, if it is null or expired, get a new one.
/// </summary>
/// <returns>string Auth token</returns>
    public async Task<string> GetAuthToken()
    {
        if (_authToken == null || _authToken.IsExpired)
        {
            // Get new token
            await RefreshAuthToken();
        }
        return _authToken!.AccessToken;
    }

  /// <summary>
    /// Refresh the auth token by making a request to the Reddit API.
    /// </summary>
    private async Task RefreshAuthToken()
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/access_token");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.ClientId}:{_config.ClientSecret}")));
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" }
            });

            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            _authToken = RedditAuthToken.FromJson(json);
        }
        catch (HttpRequestException ex)
        {
            // Log exception details here (for example, using a logging framework)
            throw new Exception("Failed to refresh Reddit auth token.", ex);
        }
        catch (Exception ex)
        {
            // Handle general exceptions
            throw new Exception("An unexpected error occurred while refreshing the Reddit auth token.", ex);
        }
    }
}
