using AssetMonitor.Domain.Enums;
using AssetMonitor.Domain.Interfaces;

namespace AssetMonitor.Application.Services;

public class MonitorEngine
{
    private readonly IStockProvider _stockProvider;
    private readonly IEmailService _emailService;
    private AlertState _lastAlert = AlertState.None;

    public MonitorEngine(IStockProvider stockProvider, IEmailService emailService)
    {
        _stockProvider = stockProvider;
        _emailService = emailService;
    }

    public async Task ProcessAssetAsync(string assetCode, decimal buyPrice, decimal sellPrice, 
        string destination, CancellationToken ct)
    {
        var quote = await _stockProvider.GetAssetPriceAsync(assetCode, ct);
        
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {assetCode}: {quote.Currency} {quote.Value}");

        if (_lastAlert != AlertState.Buy && quote.Value < buyPrice)
        {
            Console.WriteLine($"[PREÇO DE COMPRA DETECTADO] Tentando notificar...");
            await _emailService.SendAlertAsync(destination, $"COMPRA: {assetCode}", $"Preço: {quote.Value}", ct);
            _lastAlert = AlertState.Buy;
        }
        else if (_lastAlert != AlertState.Sell && quote.Value > sellPrice)
        {
            Console.WriteLine($"[PREÇO DE VENDA DETECTADO] Tentando notificar...");
            await _emailService.SendAlertAsync(destination, $"VENDA: {assetCode}", $"Preço: {quote.Value}", ct);
            _lastAlert = AlertState.Sell;
        }
        else if (quote.Value >= buyPrice && quote.Value <= sellPrice)
        {
            _lastAlert = AlertState.None;
        }
    }
}