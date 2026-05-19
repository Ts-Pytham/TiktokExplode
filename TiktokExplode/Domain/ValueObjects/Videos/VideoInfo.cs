namespace TiktokExplode.Domain.ValueObjects.Videos;

/// <summary>
/// Technical metadata about a TikTok video's encoding, dimensions, and available download URLs.
/// </summary>
public sealed record VideoInfo
{
    /// <summary>
    /// The resolution or quality label as reported by TikTok (e.g. <c>"720p"</c>, <c>"1080p"</c>).
    /// </summary>
    public string Ratio { get; init; } = string.Empty;

    /// <summary>All available bitrate and codec variants for this video.</summary>
    public Bitrate[] Bitrates { get; init; } = [];

    /// <summary>
    /// The quality tier as reported by TikTok (e.g. <c>"normal"</c>, <c>"high"</c>).
    /// </summary>
    public string VideoQuality { get; init; } = string.Empty;

    /// <summary>The pixel width of the video frame.</summary>
    public int Width { get; init; }

    /// <summary>The pixel height of the video frame.</summary>
    public int Height { get; init; }

    /// <summary>CDN URLs to download the video, both with and without watermark.</summary>
    public VideoDownloadLinks DownloadLinks { get; init; } = new();
}
