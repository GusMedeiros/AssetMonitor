using AssetMonitor.Domain.Exceptions;
using AssetMonitor.Domain.Interfaces;
using AssetMonitor.Domain.ValueObjects;

namespace AssetMonitor.Infrastructure.Providers;

public class CompositeStockProvider : IStockProvider
{
    private readonly IEnumerable<IStockProvider> _providers;

    public CompositeStockProvider(IEnumerable<IStockProvider> providers)
    {
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
    }

    public async Task<StockQuote> GetAssetPriceAsync(string assetCode, CancellationToken ct)
    {
        var errors = new List<string>();

        foreach (var provider in _providers)
        {
            try
            {
                return await provider.GetAssetPriceAsync(assetCode, ct);
            }
            catch (ProviderException ex)
            {
                var providerName = provider.GetType().Name;
                errors.Add($"{providerName}: {ex.Message}");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [AVISO] {providerName} falhou. Tentando próximo...");
            }
        }

        throw new ProviderException($"Todos os provedores falharam: {string.Join(" | ", errors)}");
    }
}