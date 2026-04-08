using Moq;
using AssetMonitor.Domain.Interfaces;
using AssetMonitor.Domain.ValueObjects;
using AssetMonitor.Application.Services;

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
        _engine = new MonitorEngine(_mockStockProvider.Object, _mockEmailService.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSendEmail_WhenPriceIsBelowBuyTarget()
    {
        // Test case: quote = BRL19, targets = [20, 30]
        _mockStockProvider
            .Setup(x => x.GetAssetPriceAsync("PETR4", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StockQuote("BRL", 19.00m));

        await _engine.ProcessAssetAsync("PETR4", 20.00m, 30.00m, 
            "exemplo@exemplo.com", CancellationToken.None);
        
        _mockEmailService.Verify(x => x.SendAlertAsync(
                "exemplo@exemplo.com", 
                It.Is<string>(subj => subj.Contains("COMPRA")),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}