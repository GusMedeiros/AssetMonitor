namespace AssetMonitor.Domain.Exceptions;

public class InvalidAssetException : Exception 
{
    public InvalidAssetException(string message, Exception? inner = null) : base(message, inner) { }
}