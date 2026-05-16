namespace TiktokExplode.Domain.Exceptions;

public class TiktokException : Exception
{
    public TiktokException()
        : base("An error occurred while processing the TikTok video.")
    {
    }
    public TiktokException(string message) : base(message)
    {
    }

    public TiktokException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}