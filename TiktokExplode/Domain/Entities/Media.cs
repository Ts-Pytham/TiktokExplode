using TiktokExplode.Domain.ValueObjects.Media;

namespace TiktokExplode.Domain.Entities;

/// <summary>
/// Base type for all TikTok media content, exposing metadata shared across videos
/// and slideshow (carousel) posts.
/// </summary>
public abstract class Media
{
    /// <summary>The unique TikTok-assigned numeric identifier for this video.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>The URL of the media on TikTok's website.</summary>
    public abstract string Url { get; }

    /// <summary>The caption or description text of the video, as written by the author.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>The author who posted the video.</summary>
    public Author Author { get; init; } = new();

    /// <summary>The detected primary language of the video's text content.</summary>
    public MediaLanguage Language { get; init; } = new();

    /// <summary>Engagement statistics for this video (views, likes, comments, etc.).</summary>
    public MediaStats Stats { get; init; } = new();

    /// <summary>The location tag associated with the video. Empty string if not set.</summary>
    public string Location { get; init; } = string.Empty;

    /// <summary>The date and time (UTC) when the video was published.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Cover thumbnail URLs for this video — static frame and animated (looping) preview.</summary>
    public MediaCover Cover { get; init; } = new();

    /// <summary>
    /// The music or sound associated with the video, including track metadata and artwork.
    /// </summary>
    public MediaMusic Music { get; init; } = new();

}
