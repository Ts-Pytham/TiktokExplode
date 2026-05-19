namespace TiktokExplode.Domain.Exceptions;

/// <summary>
/// Thrown when the requested TikTok video cannot be found —
/// either because it does not exist, has been deleted, or the account is set to private.
/// </summary>
public sealed class VideoNotFoundException : TiktokException
{
    /// <summary>Initializes a new instance with a default message.</summary>
    public VideoNotFoundException()
        : base("The specified video could not be found.")
    {
    }

    /// <summary>Initializes a new instance with the specified <paramref name="message"/>.</summary>
    public VideoNotFoundException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance with the specified <paramref name="message"/>
    /// and <paramref name="innerException"/>.
    /// </summary>
    public VideoNotFoundException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}