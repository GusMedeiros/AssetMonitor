using System.Net;
using System.Text.Json;
using Moq;
using Moq.Protected;
using AssetMonitor.Domain.Exceptions;
using AssetMonitor.Infrastructure.Providers;
using AssetMonitor.Infrastructure.Models;

namespace Test;

public class BrapiStockProviderTests
{
    private Mock<HttpMessageHandler> CreateHttpMessageHandlerMock(HttpStatusCode statusCode, string content)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            });

        return handlerMock;
    }

    [Fact]
    public async Task GetAssetPriceAsync_ShouldThrowInvalidAssetException_WhenApiReturns404()
    {
        var errorResponse = new BrapiErrorResponse { Error = true, Message = "Not found" };
        var jsonResponse = JsonSerializer.Serialize(errorResponse);
        
        var handlerMock = CreateHttpMessageHandlerMock(HttpStatusCode.NotFound, jsonResponse);
        var httpClient = new HttpClient(handlerMock.Object);
        var provider = new BrapiStockProvider(httpClient, "fake_token");

        var exception = await Assert.ThrowsAsync<InvalidAssetException>(() => 
            provider.GetAssetPriceAsync("fake_asset", CancellationToken.None));
            
        Assert.Contains("rejeitou o ativo", exception.Message);
    }

    [Fact]
    public async Task GetAssetPriceAsync_ShouldThrowProviderException_WhenApiReturns429()
    {
        var handlerMock = CreateHttpMessageHandlerMock(HttpStatusCode.TooManyRequests, "Rate limit exceeded");
        var httpClient = new HttpClient(handlerMock.Object);
        var provider = new BrapiStockProvider(httpClient, "fake_token");

        var exception = await Assert.ThrowsAsync<ProviderException>(() => 
            provider.GetAssetPriceAsync("PETR4", CancellationToken.None));
            
        Assert.Contains("Limite de requisições", exception.Message);
    }

    [Fact]
    public async Task GetAssetPriceAsync_ShouldReturnStockQuote_WhenApiReturns200OK()
    {
        var fakeResponse = new BrapiResponse 
        { 
            Results = new List<BrapiResult> { new BrapiResult { Currency = "BRL", RegularMarketPrice = 22.50m } } 
        };
        var handlerMock = CreateHttpMessageHandlerMock(HttpStatusCode.OK, JsonSerializer.Serialize(fakeResponse));
        var httpClient = new HttpClient(handlerMock.Object);
        var provider = new BrapiStockProvider(httpClient, "fake_token");

        var quote = await provider.GetAssetPriceAsync("PETR4", CancellationToken.None);

        Assert.NotNull(quote);
        Assert.Equal("BRL", quote.Currency);
        Assert.Equal(22.50m, quote.Value);
    }
}