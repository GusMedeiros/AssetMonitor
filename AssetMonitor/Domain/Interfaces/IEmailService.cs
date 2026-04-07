namespace AssetMonitor.Domain.Interfaces;

public interface IEmailService
{
    public Task SendAlertAsync(string to, string subject, string body);
}