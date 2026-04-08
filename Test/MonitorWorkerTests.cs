using AssetMonitor.Application.Services;
using AssetMonitor.Domain.Exceptions;
using AssetMonitor.Domain.Interfaces;
using AssetMonitor.Domain.ValueObjects;
using Moq;

namespace Test;

public class MonitorWorkerTests
{
    private readonly Mock<IStockProvider> _stockProviderMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly MonitorEngine _engine;
    private readonly MonitorWorker _worker;

    public MonitorWorkerTests()
    {
        _stockProviderMock = new Mock<IStockProvider>();
        _emailServiceMock = new Mock<IEmailService>();
        
        _engine = new MonitorEngine(_stockProviderMock.Object, _emailServiceMock.Object);
        _worker = new MonitorWorker(_engine);
    }
    
    // Loop interruption tests
    // Cancellation tests 
    [Fact]
    public async Task StartAsync_WhenCanceledDuringProcessing_ShouldBreakLoopGracefully()
    {
        using var cts = new CancellationTokenSource();
        
        _stockProviderMock.Setup(x => x.GetAssetPriceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => cts.Cancel()) 
            .ReturnsAsync(new StockQuote("BRL", 22.50m));

        await _worker.StartAsync("PETR4", 22.00m, 23.00m, "test@test.com", 0, cts.Token);

        _stockProviderMock.Verify(x => x.GetAssetPriceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _emailServiceMock.VerifyNoOtherCalls();
    }
    [Fact]
    public async Task StartAsync_WhenCanceledDuringApiRequest_ShouldBreakLoopGracefully()
    {
        using var cts = new CancellationTokenSource();

        _stockProviderMock.Setup(x => x.GetAssetPriceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        await _worker.StartAsync("PETR4", 22.00m, 23.00m, "test@test.com", 0, cts.Token);

        _stockProviderMock.Verify(x => x.GetAssetPriceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _emailServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StartAsync_WhenCanceledDuringEmailSending_ShouldBreakLoopGracefully()
    {
        using var cts = new CancellationTokenSource();

        _stockProviderMock.Setup(x => x.GetAssetPriceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StockQuote("BRL", 20.00m)); 

        _emailServiceMock.Setup(x => x.SendAlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        await _worker.StartAsync("PETR4", 22.00m, 23.00m, "test@test.com", 0, cts.Token);

        _stockProviderMock.Verify(x => x.GetAssetPriceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _emailServiceMock.Verify(x => x.SendAlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }
    
    // Tests for interruptions due to exceptions
    [Fact]
    public async Task StartAsync_WhenFatalInvalidAssetExceptionOccurs_ShouldBreakLoopImmediately()
    {
        using var cts = new CancellationTokenSource();
        
        _stockProviderMock.Setup(x => x.GetAssetPriceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidAssetException("Invalid asset"));

        await _worker.StartAsync("INVALID", 22.00m, 23.00m, "test@test.com", 0, cts.Token);

        _stockProviderMock.Verify(x => x.GetAssetPriceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartAsync_WhenFatalArgumentExceptionOccurs_ShouldBreakLoopImmediately()
    {
        using var cts = new CancellationTokenSource();
        
        _stockProviderMock.Setup(x => x.GetAssetPriceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid API Token"));

        await _worker.StartAsync("PETR4", 22.00m, 23.00m, "test@test.com", 0, cts.Token);

        _stockProviderMock.Verify(x => x.GetAssetPriceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartAsync_WhenTransientProviderExceptionOccurs_ShouldCatchAndContinueLoop()
    {
        using var cts = new CancellationTokenSource();
        int callCount = 0;

        _stockProviderMock.Setup(x => x.GetAssetPriceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new ProviderException("Rate Limit");
                }
                
                cts.Cancel();
                return Task.FromResult(new StockQuote("BRL", 22.50m));
            });

        await _worker.StartAsync("PETR4", 22.00m, 23.00m, "test@test.com", 0, cts.Token);

        _stockProviderMock.Verify(x => x.GetAssetPriceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task StartAsync_WhenEmailServiceFailsWithInvalidOperationException_ShouldCatchAndContinueLoop()
    {
        using var cts = new CancellationTokenSource();
        int callCount = 0;

        _stockProviderMock.Setup(x => x.GetAssetPriceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StockQuote("BRL", 20.00m));

        _emailServiceMock.Setup(x => x.SendAlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new InvalidOperationException("SMTP connection failed");
                }
                
                cts.Cancel();
                return Task.CompletedTask;
            });

        await _worker.StartAsync("PETR4", 22.00m, 23.00m, "test@test.com", 0, cts.Token);

        _stockProviderMock.Verify(x => x.GetAssetPriceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _emailServiceMock.Verify(x => x.SendAlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}