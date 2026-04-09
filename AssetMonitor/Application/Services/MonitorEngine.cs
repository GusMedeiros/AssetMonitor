using AssetMonitor.Domain.Enums;
using AssetMonitor.Domain.Interfaces;
using AssetMonitor.Infrastructure.Configurations;

namespace AssetMonitor.Application.Services;

public class MonitorEngine
{
    private readonly IStockProvider _stockProvider;
    private readonly IEmailService _emailService;
    private readonly MonitorSettings _settings;
    
    private AlertState _lastAlert = AlertState.None;
    private DateTime? _lastBuyAlertTime;
    private DateTime? _lastSellAlertTime;

    public MonitorEngine(
        IStockProvider stockProvider, 
        IEmailService emailService, 
        MonitorSettings settings)
    {
        _stockProvider = stockProvider;
        _emailService = emailService;
        _settings = settings;
    }

    public async Task ProcessAssetAsync(string assetCode, decimal buyPrice, decimal sellPrice, 
        string destination, CancellationToken ct)
    {
        var quote = await _stockProvider.GetAssetPriceAsync(assetCode, ct);
        
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {assetCode}: {quote.Currency} {quote.Value}");

        if (quote.Value < buyPrice)
        {
            if (ShouldSendAlert(AlertState.Buy, _lastBuyAlertTime))
            {
                Console.WriteLine($"[PREÇO DE COMPRA DETECTADO] Tentando notificar...");
                await _emailService.SendAlertAsync(destination, $"COMPRA: {assetCode}", $"Preço: {quote.Value}", ct);
                
                _lastAlert = AlertState.Buy;
                _lastBuyAlertTime = DateTime.Now;
            }
        }
        else if (quote.Value > sellPrice)
        {
            if (ShouldSendAlert(AlertState.Sell, _lastSellAlertTime))
            {
                Console.WriteLine($"[PREÇO DE VENDA DETECTADO] Tentando notificar...");
                await _emailService.SendAlertAsync(destination, $"VENDA: {assetCode}", $"Preço: {quote.Value}", ct);
                
                _lastAlert = AlertState.Sell;
                _lastSellAlertTime = DateTime.Now;
            }
        }
        else
        {
            _lastAlert = AlertState.None;
        }
    }

    private bool ShouldSendAlert(AlertState newState, DateTime? lastAlertTime)
    {
        // Sends alert if and only if state changed from None to buy or sell and is not in cooldown.
        if (_lastAlert == newState) 
            return false;

        if (!_settings.AlertCooldownMinutes.HasValue || _settings.AlertCooldownMinutes <= 0)
        {
            return true;
        }

        if (lastAlertTime.HasValue)
        {
            var elapsed = DateTime.Now - lastAlertTime.Value;
            if (elapsed.TotalMinutes < _settings.AlertCooldownMinutes.Value)
            {
                return false;
            }
        }

        return true;
    }
}