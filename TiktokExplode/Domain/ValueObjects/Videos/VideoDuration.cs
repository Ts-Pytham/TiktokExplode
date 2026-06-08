namespace TiktokExplode.Domain.ValueObjects.Videos;

/// <summary>
/// Represents the playback duration of a TikTok video.
/// </summary>
public readonly record struct VideoDuration
{
    /// <summary>Playback duration rounded to the nearest whole second.</summary>
    public int Seconds { get; init; }

    /// <summary>
    /// Precise playback duration in seconds when TikTok provides fractional precision;
    /// otherwise the same value as <see cref="Seconds"/>.
    /// </summary>
    public double PreciseSeconds { get; init; }
}
