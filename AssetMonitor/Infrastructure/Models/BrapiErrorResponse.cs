namespace AssetMonitor.Infrastructure.Models;

public class BrapiErrorResponse
{
    public bool Error { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Code { get; set; }
}