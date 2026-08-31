namespace TiktokExplode.Domain.Exceptions;

/// <summary>
/// Thrown when the requested TikTok video cannot be found —
/// either because it does not exist, has been deleted, or the account is set to private.
/// </summary>
/// <remarks>
/// Specialization of <see cref="MediaNotFoundException"/> for video posts. Catch the base type
/// to handle photo carousels as well.
/// </remarks>
public sealed class VideoNotFoundException : MediaNotFoundException
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