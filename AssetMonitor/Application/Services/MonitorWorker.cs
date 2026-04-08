namespace AssetMonitor.Application.Services;

public class MonitorWorker
{
    private readonly MonitorEngine _engine;

    public MonitorWorker(MonitorEngine engine)
    {
        _engine = engine;
    }

    public async Task StartAsync(
        string assetCode, 
        decimal buyPrice, 
        decimal sellPrice, 
        string destination, 
        int intervalMinutes, 
        CancellationToken ct)
    {
        Console.WriteLine($"Iniciando loop de monitoramento a cada {intervalMinutes} minuto(s)...");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await _engine.ProcessAssetAsync(assetCode, buyPrice, sellPrice, destination, ct);
                await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), ct);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("\n[Worker] Encerramento solicitado.");
                break;
            }
            
        }

        Console.WriteLine("[Worker] Monitoramento encerrado de forma segura.");
    }
}