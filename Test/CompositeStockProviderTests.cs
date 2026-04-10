using AssetMonitor.Domain.Exceptions;
using AssetMonitor.Domain.Interfaces;
using AssetMonitor.Domain.ValueObjects;
using AssetMonitor.Infrastructure.Providers;
using Moq;

namespace Test;

public class CompositeStockProviderTests
{
    private readonly Mock<IStockProvider> _primaryMock;
    private readonly Mock<IStockProvider> _secondaryMock;
    private readonly Mock<IStockProvider> _tertiaryMock;

    public CompositeStockProviderTests()
    {
        _primaryMock = new Mock<IStockProvider>();
        _secondaryMock = new Mock<IStockProvider>();
        _tertiaryMock = new Mock<IStockProvider>();
    }

    [Fact]
    public async Task GetAssetPriceAsync_WhenPrimarySucceeds_ShouldReturnResultAndNotCallOthers()
    {
        var expectedQuote = new StockQuote("BRL", 25.50m);
        
        _primaryMock.Setup(x => x.GetAssetPriceAsync("PETR4", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedQuote);

        var composite = new CompositeStockProvider(new[] { _primaryMock.Object, _secondaryMock.Object });

        var result = await composite.GetAssetPriceAsync("PETR4", CancellationToken.None);

        Assert.Equal(expectedQuote.Value, result.Value);
        _primaryMock.Verify(x => x.GetAssetPriceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        
        _secondaryMock.VerifyNoOtherCalls(); 
    }

    [Fact]
    public async Task GetAssetPriceAsync_WhenPrimaryFailsWithProviderException_ShouldReturnFromSecondary()
    {
        var expectedQuote = new StockQuote("BRL", 25.50m);

        _primaryMock.Setup(x => x.GetAssetPriceAsync("PETR4", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ProviderException("Rate limit da Brapi estourou"));

        _secondaryMock.Setup(x => x.GetAssetPriceAsync("PETR4", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedQuote);

        var composite = new CompositeStockProvider(new[] { _primaryMock.Object, _secondaryMock.Object });

        var result = await composite.GetAssetPriceAsync("PETR4", CancellationToken.None);

        Assert.Equal(expectedQuote.Value, result.Value);
        _primaryMock.Verify(x => x.GetAssetPriceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _secondaryMock.Verify(x => x.GetAssetPriceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAssetPriceAsync_WhenAllProvidersFailWithProviderException_ShouldThrowAggregateProviderException()
    {
        _primaryMock.Setup(x => x.GetAssetPriceAsync("PETR4", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ProviderException("Brapi fora do ar"));

        _secondaryMock.Setup(x => x.GetAssetPriceAsync("PETR4", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ProviderException("Yahoo Finance bloqueou o IP"));

        var composite = new CompositeStockProvider(new[] { _primaryMock.Object, _secondaryMock.Object });

        var exception = await Assert.ThrowsAsync<ProviderException>(() => 
            composite.GetAssetPriceAsync("PETR4", CancellationToken.None));

        Assert.Contains("Brapi", exception.Message);
        Assert.Contains("Yahoo", exception.Message);
        
        _primaryMock.Verify(x => x.GetAssetPriceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _secondaryMock.Verify(x => x.GetAssetPriceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAssetPriceAsync_WhenFatalExceptionOccurs_ShouldBubbleUpImmediatelyWithoutCallingNext()
    {
        _primaryMock.Setup(x => x.GetAssetPriceAsync("ATIVO_FALSO", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidAssetException("Ativo ATIVO_FALSO não encontrado"));

        var composite = new CompositeStockProvider(new[] { _primaryMock.Object, _secondaryMock.Object });

        await Assert.ThrowsAsync<InvalidAssetException>(() => 
            composite.GetAssetPriceAsync("ATIVO_FALSO", CancellationToken.None));

        _primaryMock.Verify(x => x.GetAssetPriceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        
        _secondaryMock.VerifyNoOtherCalls();
    }
    
    [Fact]
    public void Constructor_WhenProvidersListIsNull_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new CompositeStockProvider(null!));
    }
}