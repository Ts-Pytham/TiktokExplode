namespace TiktokExplode.Domain.Exceptions;

/// <summary>
/// Thrown when the HTML or JSON structure returned by TikTok cannot be parsed as expected.
/// This usually means TikTok changed its page layout or data format.
/// <para>
/// This failure is never transient: retrying produces the exact same result.
/// If you hit it, the library needs updating.
/// </para>
/// </summary>
public sealed class TiktokParsingException : TiktokException
{
    /// <summary>
    /// The dotted path of the HTML selector or JSON node that could not be resolved,
    /// for example <c>__DEFAULT_SCOPE__.webapp.video-detail.itemInfo</c>.
    /// Empty when the failure is not tied to a specific location.
    /// </summary>
    public string Path { get; init; } = string.Empty;

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
