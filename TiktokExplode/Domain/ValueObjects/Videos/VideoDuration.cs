namespace TiktokExplode.Domain.ValueObjects.Videos;

/// <summary>
/// Represents the playback duration of a TikTok video.
/// </summary>
public readonly record struct VideoDuration
{
    /// <summary>Playback duration rounded to the nearest whole second.</summary>
    public int Seconds { get; init; }

    /// <summary>
    /// Precise playback duration in seconds as a fractional value (e.g. <c>15.48</c>).
    /// Sourced from the <c>music.preciseDuration.preciseDuration</c> field in TikTok's JSON.
    /// </summary>
    public double PreciseSeconds { get; init; }
}
