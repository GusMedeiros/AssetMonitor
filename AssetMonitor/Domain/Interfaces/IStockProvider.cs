using AssetMonitor.Domain.ValueObjects;

namespace AssetMonitor.Domain.Interfaces;

public interface IStockProvider
{
    Task<StockQuote> GetAssetPriceAsync(string assetCode, CancellationToken ct);
}