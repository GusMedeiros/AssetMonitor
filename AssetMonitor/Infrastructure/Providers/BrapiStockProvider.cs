using System.Net.Http.Json;
using AssetMonitor.Domain.Interfaces;
using AssetMonitor.Domain.ValueObjects;
using AssetMonitor.Infrastructure.Models;

namespace AssetMonitor.Infrastructure.Providers;

public class BrapiStockProvider : IStockProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiToken;

    public BrapiStockProvider(HttpClient httpClient, string apiToken)
    {
        _httpClient = httpClient;
        _apiToken = apiToken;
    }

    public async Task<StockQuote> GetAssetPriceAsync(string assetCode)
    {
        var url = $"https://brapi.dev/api/quote/{assetCode}?token={_apiToken}";
        var response = await _httpClient.GetFromJsonAsync<BrapiResponse>(url);
        var result = response?.Results?.FirstOrDefault();

        if (result == null) throw new Exception($"Asset '{assetCode}' not found.");

        return new StockQuote(result.Currency ?? "Moeda desconhecida", result.RegularMarketPrice);
    }
}