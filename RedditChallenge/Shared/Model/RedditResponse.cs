using System.Text.Json.Serialization;

namespace RedditChallenge.Shared.Model;

public class RateLimit{
    public int Used { get; set; }  
    public decimal Remaining { get; set; }  
    public int Reset { get; set; }  
}


public class RedditRootResponse<T>
{
    [JsonPropertyName("kind")]
    public string? Kind { get; set; }
    
    [JsonPropertyName("data")]
    public RedditResponseData<T>? Data { get; set; } 

    public RateLimit RateLimit { get; set; } = new RateLimit();

}

public class RedditResponseData<T>
{
    [JsonPropertyName("modhash")]
    public string? Modhash { get; set; }
    [JsonPropertyName("before")]
    public string? Before { get; set; }
    [JsonPropertyName("after")]
    public string? After { get; set; }
    [JsonPropertyName("dist")]
    public int Dist { get; set; } 
        
    [JsonPropertyName("geo_filter")]
    public string? GeoFilter { get; set; }

    [JsonPropertyName("children")]
    public List<T>? Children { get; set; }
}

public class SubredditResponse{
    [JsonPropertyName("kind")]
    public string? Kind { get; set; }
    [JsonPropertyName("data")]
    public SubRedditPost? Data { get; set; }
}

