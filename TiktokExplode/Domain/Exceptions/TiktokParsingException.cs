namespace TiktokExplode.Domain.Exceptions;

public sealed class TiktokParsingException : TiktokException
{
    public TiktokParsingException()
        : base("Failed to parse the TikTok response. The page structure may have changed.")
    {
    }

    public TiktokParsingException(string message) : base(message)
    {
    }

    public TiktokParsingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
