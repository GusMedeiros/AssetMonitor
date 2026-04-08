namespace AssetMonitor.Domain.Interfaces;

public interface IEmailService
{
    Task SendAlertAsync(string to, string subject, string body, CancellationToken ct);
}