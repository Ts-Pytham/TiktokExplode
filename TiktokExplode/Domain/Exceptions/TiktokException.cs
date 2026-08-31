namespace TiktokExplode.Domain.Exceptions;

/// <summary>
/// Base class for all exceptions thrown by the TiktokExplode library.
/// Catch this type to handle any library-specific error in a single block.
/// </summary>
public class TiktokException : Exception
{
    /// <summary>
    /// Indicates whether the failure is expected to disappear on its own if the same
    /// request is issued again (WAF challenges, soft-blocks, rate limiting, server errors).
    /// <para>
    /// The library only retries exceptions for which this returns <see langword="true"/>.
    /// Consumers can read it to build their own retry policy without knowing the exception hierarchy.
    /// </para>
    /// </summary>
    public virtual bool IsTransient => false;

    /// <summary>Initializes a new instance with a default message.</summary>
    public TiktokException()
        : base("An error occurred while processing the TikTok video.")
    {
    }

    /// <summary>Initializes a new instance with the specified <paramref name="message"/>.</summary>
    public TiktokException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance with the specified <paramref name="message"/>
    /// and <paramref name="innerException"/>.
    /// </summary>
    public TiktokException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}