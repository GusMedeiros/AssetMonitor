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

    public async Task SendAlertAsync(string to, string subject, string body)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Monitor B3 (Desafio Inoa)", _settings.Username));
        message.To.Add(new MailboxAddress("", to));
        message.Subject = subject;

        message.Body = new TextPart("plain") { Text = body };
        
        using var client = new SmtpClient();
        
        try
        {
            await client.ConnectAsync(_settings.SmtpServer, _settings.SmtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_settings.Username, _settings.Password);
            await client.SendAsync(message);
            
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] E-mail de alerta enviado com sucesso para {to}!");
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(true);
            }
        }
    }
}