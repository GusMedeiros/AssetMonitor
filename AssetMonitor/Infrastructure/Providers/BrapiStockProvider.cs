using System.Net;
using System.Net.Http.Json;
using AssetMonitor.Domain.Exceptions;
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

    public async Task<StockQuote> GetAssetPriceAsync(string assetCode, CancellationToken ct)
    {
        var url = $"https://brapi.dev/api/quote/{assetCode}?token={_apiToken}";

        try
        {
            using var response = await _httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                await HandleErrorResponseAsync(response, assetCode, ct);
            }

            return await ParseSuccessResponseAsync(response, assetCode, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new ProviderException("Falha de conexão física/rede ao tentar acessar a Brapi.", ex);
        }
    }
    
    private async Task HandleErrorResponseAsync(HttpResponseMessage response, string assetCode, CancellationToken ct)
    {
        string apiMessage = await ExtractErrorMessageAsync(response, ct);

        throw response.StatusCode switch
        {
            HttpStatusCode.NotFound or HttpStatusCode.BadRequest => 
                new InvalidAssetException($"A API rejeitou o ativo '{assetCode}'. Detalhe: {apiMessage}"),
                
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => 
                new ArgumentException($"[FALHA CRÍTICA] Erro de Autenticação na API (Token Inválido). Detalhe: {apiMessage}"),
                
            HttpStatusCode.TooManyRequests => 
                new ProviderException($"Limite de requisições excedido (Rate Limit). Tentaremos no próximo ciclo. Detalhe: {apiMessage}"),
                
            _ => new ProviderException($"Instabilidade na API da Brapi. Tentaremos no próximo ciclo. Detalhe: {apiMessage}")
        };
    }

    private async Task<string> ExtractErrorMessageAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var errorData = await response.Content.ReadFromJsonAsync<BrapiErrorResponse>(cancellationToken: ct);
            return !string.IsNullOrWhiteSpace(errorData?.Message) ? errorData.Message : $"HTTP {response.StatusCode}";
        }
        catch
        {
            return $"HTTP {response.StatusCode}";
        }
    }

    private async Task<StockQuote> ParseSuccessResponseAsync(HttpResponseMessage response, string assetCode, CancellationToken ct)
    {
        var data = await response.Content.ReadFromJsonAsync<BrapiResponse>(cancellationToken: ct);
        var result = data?.Results?.FirstOrDefault();

        if (result == null) 
        {
            throw new InvalidAssetException($"O ativo '{assetCode}' não trouxe nenhum resultado na lista.");
        }

        return new StockQuote(result.Currency ?? "BRL", result.RegularMarketPrice);
    }
}