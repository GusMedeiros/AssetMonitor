using System;
using System.Threading;
using System.Threading.Tasks;
using AssetMonitor.Domain.Interfaces;
using AssetMonitor.Infrastructure.Configurations;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace AssetMonitor.Infrastructure.Notifications;

public class MailKitEmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public MailKitEmailService(EmailSettings settings)
    {
        _settings = settings;
    }

    public async Task SendAlertAsync(string to, string subject, string body, CancellationToken ct)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Monitor B3 (Desafio Inoa)", _settings.Username));
        message.To.Add(new MailboxAddress("", to));
        message.Subject = subject;

        message.Body = new TextPart("plain") { Text = body };
        
        using var client = new SmtpClient();
        
        try
        {
            await client.ConnectAsync(_settings.SmtpServer, _settings.SmtpPort, SecureSocketOptions.StartTls, ct);
            await client.AuthenticateAsync(_settings.Username, _settings.Password, ct);
            await client.SendAsync(message, ct);
            
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] E-mail de alerta enviado com sucesso para {to}!");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [AVISO] O envio do e-mail para {to} foi cancelado pela aplicação.");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [ERRO] Falha ao enviar o e-mail. Detalhe: {ex.Message}");
            throw new InvalidOperationException("Não foi possível enviar o e-mail de alerta.", ex);
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(true, ct);
            }
        }
    }
}