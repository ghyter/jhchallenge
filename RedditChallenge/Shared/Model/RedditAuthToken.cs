using System.Text.Json.Serialization;
namespace  RedditChallenge.Shared.Model;

public class RedditAuthToken
{
    public string AccessToken { get; }
    public string TokenType { get; }
    public int ExpiresIn { get; }
    public DateTime CreatedAt { get; }
    public string Scope { get; }

    public bool IsExpired => CreatedAt.AddSeconds(ExpiresIn) < DateTime.UtcNow;

    // Constructor annotated with JsonConstructor to support deserialization
    [JsonConstructor]
    public RedditAuthToken(string accessToken, string tokenType, int expiresIn, string scope, DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new ArgumentException("AccessToken cannot be null or empty", nameof(accessToken));
        }
        if (string.IsNullOrWhiteSpace(tokenType))
        {
            throw new ArgumentException("TokenType cannot be null or empty", nameof(tokenType));
        }
        if (expiresIn <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expiresIn), "ExpiresIn must be greater than zero");
        }
        if (string.IsNullOrWhiteSpace(scope))
        {
            throw new ArgumentException("Scope cannot be null or empty", nameof(scope));
        }

        AccessToken = accessToken;
        TokenType = tokenType;
        ExpiresIn = expiresIn;
        Scope = scope;
        CreatedAt = createdAt;
    }

    // Method to deserialize JSON response into RedditAuthToken
    public static RedditAuthToken FromJson(string json)
    {
        try
        {
            var token = System.Text.Json.JsonSerializer.Deserialize<RedditAuthTokenJsonRecord>(json, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                 PropertyNameCaseInsensitive = true
            });

            if (token == null)
            {
                throw new System.Text.Json.JsonException("Deserialization returned null");
            }

            return new RedditAuthToken(token.AccessToken, token.TokenType, token.ExpiresIn, token.Scope, DateTime.UtcNow);
        }
        catch (System.Text.Json.JsonException ex)
        {
            // Handle deserialization exceptions (e.g., log the error or rethrow with additional context)
            throw new InvalidOperationException("Failed to deserialize RedditAuthToken from JSON", ex);
        }
    }

    // Helper class for JSON deserialization
    private record RedditAuthTokenJsonRecord(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("token_type")] string TokenType,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("scope")] string Scope);
}

