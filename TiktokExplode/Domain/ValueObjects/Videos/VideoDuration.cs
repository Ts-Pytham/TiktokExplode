namespace TiktokExplode.Domain.ValueObjects.Videos;

public readonly record struct VideoDuration
{
    public int Seconds { get; init; }
    public double PreciseSeconds { get; init; }
}
