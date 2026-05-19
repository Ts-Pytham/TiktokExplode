namespace TiktokExplode.Domain.ValueObjects;

/// <summary>
/// Wraps a video content stream with its exact byte length as reported by the CDN response.
/// Implements <see cref="IAsyncDisposable"/> — the underlying <see cref="Stream"/> is disposed
/// when this object is disposed.
/// </summary>
public sealed class StreamInfo : IAsyncDisposable
{
    /// <summary>
    /// The video content stream returned from the CDN. Must be disposed after use.
    /// </summary>
    public required Stream Stream { get; init; }

    /// <summary>
    /// The exact number of bytes in <see cref="Stream"/>, taken from the CDN
    /// <c>Content-Length</c> response header.
    /// Returns <c>-1</c> if the server did not include a <c>Content-Length</c> header.
    /// </summary>
    public required long ContentLength { get; init; }

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => Stream.DisposeAsync();
}
