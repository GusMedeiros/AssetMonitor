using AssetMonitor.Domain.Enums;
using AssetMonitor.Domain.Interfaces;
using AssetMonitor.Domain.ValueObjects;

namespace AssetMonitor.Application.Services;

public class MonitorEngine
{
    private readonly IStockProvider _stockProvider;
    private readonly IEmailService _emailService;

    public MonitorEngine(IStockProvider stockProvider, IEmailService emailService)
    {
        _stockProvider = stockProvider;
        _emailService = emailService;
    }

    public async Task ProcessAssetAsync(string assetCode, decimal buyPrice, decimal sellPrice, string alertDestination)
    {
        throw new NotImplementedException();

    }
}