using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using AssetMonitor.Application.Services;
using AssetMonitor.Domain.Interfaces;
using AssetMonitor.Infrastructure.Providers;
using AssetMonitor.Infrastructure.Configurations;
using AssetMonitor.Infrastructure.Notifications;

// Args validation
if (args.Length < 3)
{
    Console.WriteLine("Uso correto: AssetMonitor <ativo> <preco_venda> <preco_compra>");
    Console.WriteLine("Exemplo: AssetMonitor PETR4 22.67 22.59");
    return;
}

string assetCode = args[0].ToUpper();

if (!decimal.TryParse(args[1], out decimal sellPrice) || !decimal.TryParse(args[2], out decimal buyPrice))
{
    Console.WriteLine("Erro: Os preços de venda e compra devem ser números válidos.");
    return;
}

if (buyPrice >= sellPrice)
{
    Console.WriteLine("Aviso: O preço de compra está maior ou igual ao de venda. Verifique os parâmetros.");
}

// DI and configs setup
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

string? brapiToken = builder.Configuration["Brapi:Token"];
EmailSettings? emailSettings = builder.Configuration.GetSection("EmailSettings").Get<EmailSettings>();

// Config validation
if (string.IsNullOrEmpty(brapiToken))
{
    Console.WriteLine($"[ERRO CRÍTICO] Token da Brapi não encontrado! Verifique o arquivo appsettings.{builder.Environment.EnvironmentName}.json");
    return;
}

if (emailSettings == null || string.IsNullOrEmpty(emailSettings.Username))
{
    Console.WriteLine($"[ERRO CRÍTICO] Configurações de E-mail ausentes no ambiente '{builder.Environment.EnvironmentName}'.");
    return;
}

builder.Services.AddHttpClient<IStockProvider, BrapiStockProvider>(client => 
    new BrapiStockProvider(client, brapiToken));
builder.Services.AddSingleton<IEmailService>(new MailKitEmailService(emailSettings));

builder.Services.AddSingleton<MonitorEngine>();
builder.Services.AddTransient<MonitorWorker>();

using IHost host = builder.Build();

MonitorWorker worker = host.Services.GetRequiredService<MonitorWorker>();
CancellationTokenSource cts = new CancellationTokenSource();


// CLI presentation
Console.CancelKeyPress += (sender, eventArgs) =>
{
    Console.WriteLine("\nSinal de encerramento recebido (Ctrl+C). Fechando processos com segurança...");
    eventArgs.Cancel = true;
    cts.Cancel();
};
Console.WriteLine($"Ambiente Atual: {builder.Environment.EnvironmentName}");
Console.WriteLine($"--- Monitoramento Iniciado: {assetCode} ---");
Console.WriteLine($"Alvo de Venda: maior que {sellPrice}");
Console.WriteLine($"Alvo de Compra: menor que {buyPrice}");
Console.WriteLine($"Destinatário: {emailSettings.Recipient}");
Console.WriteLine("Pressione Ctrl+C para encerrar...\n");

// Run
int delayMinutes = 5;

await worker.StartAsync(assetCode, buyPrice, sellPrice, emailSettings.Recipient, delayMinutes, cts.Token);