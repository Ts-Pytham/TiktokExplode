namespace TiktokExplode.Domain.ValueObjects.Videos;

/// <summary>
/// Contains CDN download URLs and size metadata for a TikTok video.
/// </summary>
public sealed record VideoDownloadLinks
{
    /// <summary>
    /// CDN URL to download the video without a watermark.
    /// This is the URL used by <c>IVideoClient.DownloadAsync</c>.
    /// </summary>
    public string OriginalUrl { get; init; } = string.Empty;

    /// <summary>
    /// CDN URL to download the video with TikTok's watermark overlay.
    /// This is the URL used by <c>IVideoClient.DownloadWatermarkedAsync</c>.
    /// </summary>
    public string WatermarkedUrl { get; init; } = string.Empty;

    /// <summary>
    /// File size in bytes of the original (no-watermark) video as reported in TikTok's JSON metadata.
    /// This value may differ slightly from the actual CDN download size;
    /// for exact transfer sizes use <c>StreamInfo.ContentLength</c>.
    /// </summary>
    public long OriginalSizeInBytes { get; init; }
}
