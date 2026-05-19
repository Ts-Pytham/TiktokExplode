namespace TiktokExplode.Domain.Exceptions;

/// <summary>
/// Thrown when TikTok's Web Application Firewall (WAF) blocks the request.
/// This exception is raised after all automatic retry attempts have been exhausted,
/// as configured by <c>TikTokOptions.MaxWafRetries</c>.
/// </summary>
public sealed class TiktokWafException : TiktokException
{
    /// <summary>Initializes a new instance with a default message.</summary>
    public TiktokWafException()
        : base("The request was blocked by TikTok's Web Application Firewall (WAF).")
    {
    }

    /// <summary>Initializes a new instance with the specified <paramref name="message"/>.</summary>
    public TiktokWafException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance with the specified <paramref name="message"/>
    /// and <paramref name="innerException"/>.
    /// </summary>
    public TiktokWafException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}