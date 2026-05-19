namespace TiktokExplode.Domain.Exceptions;

/// <summary>
/// Thrown when the HTML or JSON structure returned by TikTok cannot be parsed as expected.
/// This usually means TikTok changed its page layout or data format.
/// </summary>
public sealed class TiktokParsingException : TiktokException
{
    /// <summary>Initializes a new instance with a default message.</summary>
    public TiktokParsingException()
        : base("Failed to parse the TikTok response. The page structure may have changed.")
    {
    }

    /// <summary>Initializes a new instance with the specified <paramref name="message"/>.</summary>
    public TiktokParsingException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance with the specified <paramref name="message"/>
    /// and <paramref name="innerException"/>.
    /// </summary>
    public TiktokParsingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
