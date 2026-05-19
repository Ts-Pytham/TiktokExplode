namespace TiktokExplode.Domain.ValueObjects;

public sealed class StreamInfo : IAsyncDisposable
{
    public required Stream Stream { get; init; }
    public required long ContentLength { get; init; }

    public ValueTask DisposeAsync() => Stream.DisposeAsync();
}
