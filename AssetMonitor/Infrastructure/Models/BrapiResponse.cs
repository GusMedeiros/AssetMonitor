using System.Text.Json.Serialization;

namespace AssetMonitor.Infrastructure.Models;

public class BrapiResponse
{
    [JsonPropertyName("results")]
    public List<BrapiResult>? Results { get; set; }
}

public class BrapiResult
{
    [JsonPropertyName("regularMarketPrice")]
    public decimal RegularMarketPrice { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }
}