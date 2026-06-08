using TiktokExplode.Domain.ValueObjects.Videos;

namespace TiktokExplode.Domain.Entities;

/// <summary>
/// Represents a TikTok video with all its associated metadata.
/// This is the root entity returned by <c>IVideoClient.GetVideoAsync</c>.
/// </summary>
public sealed class Video
{
    /// <summary>The unique TikTok-assigned numeric identifier for this video.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>The caption or description text of the video, as written by the author.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>The author who posted the video.</summary>
    public Author Author { get; init; } = new();

    /// <summary>The detected primary language of the video's text content.</summary>
    public VideoLanguage Language { get; init; } = new();

    /// <summary>Engagement statistics for this video (views, likes, comments, etc.).</summary>
    public VideoStats Stats { get; init; } = new();

    /// <summary>Technical information about the video (resolution, bitrates, download URLs).</summary>
    public VideoInfo Info { get; init; } = new();

    /// <summary>The location tag associated with the video. Empty string if not set.</summary>
    public string Location { get; init; } = string.Empty;

    /// <summary>The date and time (UTC) when the video was published.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>The playback duration of the video.</summary>
    public VideoDuration Duration { get; init; }

    /// <summary>Cover thumbnail URLs for this video — static frame and animated (looping) preview.</summary>
    public VideoCover Cover { get; init; } = new();

    /// <summary>
    /// The music or sound associated with the video, including track metadata and artwork.
    /// </summary>
    public VideoMusic Music { get; init; } = new();
}
