namespace AssetMonitor.Infrastructure.Configurations;

public record EmailSettings(
    string Recipient,
    string SmtpServer,
    int SmtpPort,
    string Username,
    string Password
);