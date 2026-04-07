using AssetMonitor.Domain.ValueObjects;

namespace AssetMonitor.Domain.Interfaces;

public interface IStockProvider
{
    public Task<StockQuote> GetAssetPrice(string assetCode);
}