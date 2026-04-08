using System;
using System.Threading;
using System.Threading.Tasks;
using AssetMonitor.Domain.Exceptions;

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
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("\n[Worker] Encerramento solicitado.");
                break;
            }
            catch (InvalidAssetException ex)
            {
                Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] [ERRO CRÍTICO] {ex.Message}");
                Console.WriteLine("[Worker] Parando monitoramento devido a ativo inválido.");
                break;
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] [ERRO CRÍTICO] Falha de configuração: {ex.Message}");
                Console.WriteLine("[Worker] Parando monitoramento.");
                break;
            }
            catch (ProviderException ex)
            {
                Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] [AVISO] {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] [AVISO] Falha no envio de e-mail: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] [ERRO FATAL] Ocorreu um erro inesperado: {ex.Message}");
                break;
            }

            if (ct.IsCancellationRequested) break;

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), ct);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("\n[Worker] Encerramento solicitado durante a espera.");
                break;
            }
        }

        Console.WriteLine("[Worker] Monitoramento encerrado de forma segura.");
    }
}