using System.Text.Json.Serialization;
namespace RedditChallenge.Shared.Model;

public class SubRedditPost
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("author")]
    public string? Author { get; set; }

    [JsonPropertyName("ups")]
    public int? Ups { get; set; }

    [JsonPropertyName("created_utc")]
    public decimal CreatedUtc { get; set; }

    public DateTime CreatedUtcDateTime => DateTimeOffset.FromUnixTimeSeconds((long)CreatedUtc).DateTime;
}