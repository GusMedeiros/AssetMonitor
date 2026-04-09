namespace AssetMonitor.Infrastructure.Configurations;

public record MonitorSettings(
    int? AlertCooldownMinutes
);