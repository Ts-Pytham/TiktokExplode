using TiktokExplode.Domain.ValueObjects.Media;
using TiktokExplode.Domain.ValueObjects.Videos;

namespace TiktokExplode.Domain.Entities;

/// <summary>
/// Represents a TikTok video with all its associated metadata.
/// This is the root entity returned by <c>IVideoClient.GetVideoAsync</c>.
/// </summary>
public sealed class Video : Media
{
    /// <summary>Technical information about the video (resolution, bitrates, download URLs).</summary>
    public VideoInfo Info { get; init; } = new();

    /// <summary>The playback duration of the video.</summary>
    public VideoDuration Duration { get; init; }
}
