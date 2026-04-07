using AssetMonitor.Domain.Enums;
using AssetMonitor.Domain.Interfaces;
using AssetMonitor.Domain.ValueObjects;

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

    public async Task ProcessAssetAsync(string assetCode, decimal buyPrice, decimal sellPrice, string alertDestination)
    {
        StockQuote quote = await _stockProvider.GetAssetPriceAsync(assetCode);
        if (_lastAlert != AlertState.Buy && quote.Value < buyPrice)
        {
            await _emailService.SendAlertAsync(alertDestination,
                $"Recomendação de COMPRA: {assetCode}",
                $"O ativo {assetCode} caiu para {quote.Currency} {quote.Value}. Alvo: {buyPrice}");
            _lastAlert = AlertState.Buy;
        }
        else if (_lastAlert != AlertState.Sell && quote.Value > sellPrice)
        {
            await _emailService.SendAlertAsync(alertDestination,
                $"Recomendação de VENDA: {assetCode}",
                $"O ativo {assetCode} subiu para {quote.Currency} {quote.Value}. Alvo: {sellPrice}");
            _lastAlert = AlertState.Sell;
        }
        else if (quote.Value >= buyPrice && quote.Value <= sellPrice)
        {
            _lastAlert = AlertState.None;
        }
    }
}