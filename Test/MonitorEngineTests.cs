using Moq;
using Xunit;
using AssetMonitor.Domain.Interfaces;
using AssetMonitor.Domain.ValueObjects;
using AssetMonitor.Application.Services;
using AssetMonitor.Domain.Exceptions;
using AssetMonitor.Infrastructure.Configurations;

namespace Test;

public class MonitorEngineTests
{
    private readonly Mock<IStockProvider> _mockStockProvider;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly MonitorEngine _engine;

    public MonitorEngineTests()
    {
        _mockStockProvider = new Mock<IStockProvider>();
        _mockEmailService = new Mock<IEmailService>();
        _engine = new MonitorEngine(_mockStockProvider.Object, _mockEmailService.Object, new MonitorSettings(0));
    }

    [Fact]
    public async Task ProcessAssetAsync_ShouldSendEmail_WhenPriceIsBelowBuyTarget()
    {
        _mockStockProvider
            .Setup(x => x.GetAssetPriceAsync("PETR4", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StockQuote("BRL", 19.00m));

        await _engine.ProcessAssetAsync("PETR4", 20.00m, 30.00m, "exemplo@exemplo.com", CancellationToken.None);
        
        _mockEmailService.Verify(x => x.SendAlertAsync(
            "exemplo@exemplo.com", 
            It.Is<string>(subj => subj.Contains("COMPRA")),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAssetAsync_ShouldSendEmail_WhenPriceIsAboveSellTarget()
    {
        _mockStockProvider
            .Setup(x => x.GetAssetPriceAsync("PETR4", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StockQuote("BRL", 31.00m));

        await _engine.ProcessAssetAsync("PETR4", 20.00m, 30.00m, "exemplo@exemplo.com", CancellationToken.None);
        
        _mockEmailService.Verify(x => x.SendAlertAsync(
            "exemplo@exemplo.com", 
            It.Is<string>(subj => subj.Contains("VENDA")),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAssetAsync_ShouldNotSendEmail_WhenPriceIsBetweenTargets()
    {
        _mockStockProvider
            .Setup(x => x.GetAssetPriceAsync("PETR4", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StockQuote("BRL", 25.00m));

        await _engine.ProcessAssetAsync("PETR4", 20.00m, 30.00m, "exemplo@exemplo.com", CancellationToken.None);
        
        _mockEmailService.Verify(x => x.SendAlertAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAssetAsync_ShouldNotSendDuplicateEmails_ForConsecutiveBuyAlerts()
    {
        _mockStockProvider
            .SetupSequence(x => x.GetAssetPriceAsync("PETR4", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StockQuote("BRL", 19.00m))
            .ReturnsAsync(new StockQuote("BRL", 18.00m));

        await _engine.ProcessAssetAsync("PETR4", 20.00m, 30.00m, "exemplo@exemplo.com", CancellationToken.None);
        await _engine.ProcessAssetAsync("PETR4", 20.00m, 30.00m, "exemplo@exemplo.com", CancellationToken.None);

        _mockEmailService.Verify(x => x.SendAlertAsync(
            It.IsAny<string>(), It.Is<string>(subj => subj.Contains("COMPRA")), It.IsAny<string>(), It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task ProcessAssetAsync_ShouldNotSendDuplicateEmails_ForConsecutiveSellAlerts()
    {
        _mockStockProvider
            .SetupSequence(x => x.GetAssetPriceAsync("PETR4", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StockQuote("BRL", 31.00m))
            .ReturnsAsync(new StockQuote("BRL", 35.00m));

        await _engine.ProcessAssetAsync("PETR4", 20.00m, 30.00m, "exemplo@exemplo.com", CancellationToken.None);
        await _engine.ProcessAssetAsync("PETR4", 20.00m, 30.00m, "exemplo@exemplo.com", CancellationToken.None);

        _mockEmailService.Verify(x => x.SendAlertAsync(
            It.IsAny<string>(), It.Is<string>(subj => subj.Contains("VENDA")), It.IsAny<string>(), It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task ProcessAssetAsync_ShouldSendBuyEmailAgain_AfterPriceReturnsToNeutralZone()
    {
        _mockStockProvider
            .SetupSequence(x => x.GetAssetPriceAsync("PETR4", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StockQuote("BRL", 19.00m))
            .ReturnsAsync(new StockQuote("BRL", 25.00m))
            .ReturnsAsync(new StockQuote("BRL", 18.00m));

        await _engine.ProcessAssetAsync("PETR4", 20.00m, 30.00m, "exemplo@exemplo.com", CancellationToken.None);
        await _engine.ProcessAssetAsync("PETR4", 20.00m, 30.00m, "exemplo@exemplo.com", CancellationToken.None);
        await _engine.ProcessAssetAsync("PETR4", 20.00m, 30.00m, "exemplo@exemplo.com", CancellationToken.None);

        _mockEmailService.Verify(x => x.SendAlertAsync(
            It.IsAny<string>(), It.Is<string>(subj => subj.Contains("COMPRA")), It.IsAny<string>(), It.IsAny<CancellationToken>()), 
            Times.Exactly(2));
    }
    
    // Tests for "sad paths"
    [Fact]
    public async Task ProcessAssetAsync_ShouldThrowInvalidAssetException_WhenAssetIsNotFound()
    {
        _mockStockProvider
            .Setup(x => x.GetAssetPriceAsync("PETR", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidAssetException("Ativo não encontrado"));

        await Assert.ThrowsAsync<InvalidAssetException>(() => 
            _engine.ProcessAssetAsync("PETR", 20.00m, 30.00m, "exemplo@exemplo.com", CancellationToken.None));
            
        _mockEmailService.Verify(x => x.SendAlertAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAssetAsync_ShouldThrowProviderException_WhenApiFails()
    {
        _mockStockProvider
            .Setup(x => x.GetAssetPriceAsync("PETR4", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ProviderException("Rate limit excedido"));

        await Assert.ThrowsAsync<ProviderException>(() => 
            _engine.ProcessAssetAsync("PETR4", 20.00m, 30.00m, "exemplo@exemplo.com", CancellationToken.None));
    }
}