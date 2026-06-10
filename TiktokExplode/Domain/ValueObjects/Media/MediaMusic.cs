using TiktokExplode.Domain.ValueObjects.Authors;

namespace TiktokExplode.Domain.ValueObjects.Media;

/// <summary>
/// Represents the music or sound associated with a TikTok media.
/// </summary>
public sealed class MediaMusic
{
    /// <summary>The unique TikTok-assigned identifier for the music or sound.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>The album name associated with the music, if available.</summary>
    public string AlbumName { get; init; } = string.Empty;

    /// <summary>The title of the music or original sound.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>The display name of the music author or original sound creator.</summary>
    public string AuthorName { get; init; } = string.Empty;

    /// <summary>Image URLs associated with the music in multiple resolutions.</summary>
    public ProfileImageVariants Images { get; init; } = new();

    /// <summary>The duration of the associated music or sound.</summary>
    public MediaDuration Duration { get; init; }

    /// <summary>
    /// <see langword="true"/> if TikTok marks the music as copyrighted;
    /// otherwise <see langword="false"/>.
    /// </summary>
    public bool IsCopyrighted { get; init; }

    /// <summary>
    /// <see langword="true"/> if this is an original sound;
    /// otherwise <see langword="false"/>.
    /// </summary>
    public bool IsOriginal { get; init; }

    /// <summary>
    /// <see langword="true"/> if the music is private or unavailable;
    /// otherwise <see langword="false"/>.
    /// </summary>
    public bool IsPrivate { get; init; }

    /// <summary>CDN URL used to play the music or sound audio, if available.</summary>
    public string PlayUrl { get; init; } = string.Empty;
}
