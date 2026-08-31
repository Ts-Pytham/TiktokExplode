namespace TiktokExplode.Domain.Exceptions;

/// <summary>
/// Thrown when a request to TikTok or its CDN completes with a non-success HTTP status code.
/// <para>
/// Whether the failure is worth retrying depends on <see cref="StatusCode"/>: <c>408</c>,
/// <c>425</c>, <c>429</c> and any <c>5xx</c> are treated as
/// <see cref="TiktokException.IsTransient">transient</see>; every other status is terminal.
/// </para>
/// </summary>
public sealed class TiktokHttpException : TiktokException
{
    /// <summary>
    /// The HTTP status code returned by the server, or <c>0</c> when the transport
    /// failed before a status line was received.
    /// </summary>
    public int StatusCode { get; init; }

    /// <inheritdoc/>
    /// <remarks>
    /// <c>0</c> (no response) is treated as transient, since it usually means a dropped
    /// connection rather than a rejected request.
    /// </remarks>
    public override bool IsTransient => StatusCode is 0 or 408 or 425 or 429 or (>= 500 and <= 599);

    /// <summary>Initializes a new instance with a default message.</summary>
    public TiktokHttpException()
        : base("TikTok responded with a non-success HTTP status code.")
    {
    }

    /// <summary>Initializes a new instance with the specified <paramref name="message"/>.</summary>
    public TiktokHttpException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance with the specified <paramref name="message"/>
    /// and <paramref name="innerException"/>.
    /// </summary>
    public TiktokHttpException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Creates an exception describing a failed request to <paramref name="url"/>
    /// that returned <paramref name="statusCode"/>.
    /// </summary>
    /// <param name="statusCode">The HTTP status code received, or <c>0</c> when no response arrived.</param>
    /// <param name="url">The URL that was requested.</param>
    public static TiktokHttpException FromStatus(int statusCode, string url)
        => new($"Request to '{url}' failed with HTTP status {statusCode}.") { StatusCode = statusCode };
}
