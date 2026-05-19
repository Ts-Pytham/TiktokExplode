namespace TiktokExplode.Domain.ValueObjects.Videos;

/// <summary>
/// Aggregated engagement statistics for a TikTok video.
/// </summary>
public sealed record VideoStats
{
    /// <summary>Total number of times the video has been played.</summary>
    public long Views { get; init; }

    /// <summary>Total number of likes ("hearts") the video has received.</summary>
    public long Likes { get; init; }

    /// <summary>Total number of comments posted on the video.</summary>
    public long Comments { get; init; }

    /// <summary>Total number of times the video has been shared.</summary>
    public long Shares { get; init; }

    /// <summary>Total number of times the video has been saved to a favorites collection.</summary>
    public long Favorites { get; init; }

    /// <summary>Total number of times the video has been reposted by other users.</summary>
    public long Reposts { get; init; }
}
