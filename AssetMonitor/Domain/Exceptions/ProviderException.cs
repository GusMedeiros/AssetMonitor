namespace AssetMonitor.Domain.Exceptions;

public class ProviderException : Exception 
{
    public ProviderException(string message, Exception? inner = null) : base(message, inner) { }
}