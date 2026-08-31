namespace TiktokExplode.Domain.Exceptions;

/// <summary>
/// Thrown when TikTok explicitly reports that the requested media does not exist,
/// was deleted, or belongs to a private account.
/// <para>
/// This is a terminal failure: TikTok answered the question, and the answer was "no".
/// It is never retried. Do not confuse it with
/// <see cref="TiktokUnavailablePageException"/>, which means TikTok declined to answer.
/// </para>
/// </summary>
public class MediaNotFoundException : TiktokException
{
    /// <summary>
    /// The <c>statusCode</c> reported by TikTok's hydration payload, when available.
    /// Values in the <c>102xx</c> range identify why the item is unavailable
    /// (removed, private, region-locked, …). <c>0</c> when TikTok did not report one.
    /// </summary>
    public int TiktokStatusCode { get; init; }

    /// <summary>Initializes a new instance with a default message.</summary>
    public MediaNotFoundException()
        : base("The specified media could not be found.")
    {
    }

    /// <summary>Initializes a new instance with the specified <paramref name="message"/>.</summary>
    public MediaNotFoundException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance with the specified <paramref name="message"/>
    /// and <paramref name="innerException"/>.
    /// </summary>
    public MediaNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
