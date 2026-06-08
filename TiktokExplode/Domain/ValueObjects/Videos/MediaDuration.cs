namespace TiktokExplode.Domain.ValueObjects.Videos;

/// <summary>
/// Represents the duration of a TikTok media resource, including a whole-second value
/// and a precise fractional value when TikTok provides one.
/// </summary>
public readonly record struct MediaDuration
{
    /// <summary>
    /// Gets the duration in whole seconds.
    /// </summary>
    public int Seconds { get; init; }

    /// <summary>
    /// Gets the duration in seconds, including fractional precision when TikTok provides it;
    /// otherwise the same value as <see cref="Seconds"/>.
    /// </summary>
    public double PreciseSeconds { get; init; }
}
