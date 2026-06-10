namespace TiktokExplode.Domain.ValueObjects.Media;

/// <summary>
/// Aggregated engagement statistics for a TikTok media.
/// </summary>
public sealed record MediaStats
{
    /// <summary>Total number of times the media has been played.</summary>
    public long Views { get; init; }

    /// <summary>Total number of likes ("hearts") the media has received.</summary>
    public long Likes { get; init; }

    /// <summary>Total number of comments posted on the media.</summary>
    public long Comments { get; init; }

    /// <summary>Total number of times the media has been shared.</summary>
    public long Shares { get; init; }

    /// <summary>Total number of times the media has been saved to a favorites collection.</summary>
    public long Favorites { get; init; }

    /// <summary>Total number of times the media has been reposted by other users.</summary>
    public long Reposts { get; init; }
}
