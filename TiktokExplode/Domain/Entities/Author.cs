using TiktokExplode.Domain.ValueObjects.Authors;

namespace TiktokExplode.Domain.Entities;

/// <summary>
/// Represents a TikTok account that authored a video.
/// </summary>
public sealed class Author
{
    /// <summary>The internal TikTok user identifier (numeric string).</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// The account handle (username) as it appears in TikTok URLs
    /// (e.g. <c>johndoe</c> in <c>https://www.tiktok.com/@johndoe</c>).
    /// </summary>
    public string UniqueId { get; init; } = string.Empty;

    /// <summary>The display name shown on the author's profile page.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>The biography text from the author's profile. Empty string if not set.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// <see langword="true"/> if the account holds TikTok's verified badge;
    /// otherwise <see langword="false"/>.
    /// </summary>
    public bool IsVerified { get; init; }

    /// <summary>
    /// <see langword="true"/> if the account is set to private;
    /// otherwise <see langword="false"/>.
    /// </summary>
    public bool IsPrivate { get; init; }

    /// <summary>The date and time (UTC) when the account was created.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Avatar image URLs in multiple resolutions.</summary>
    public ProfileImageVariants Avatar { get; init; } = new();

    /// <summary>Follower, following, and engagement statistics for this author.</summary>
    public AuthorStats Stats { get; init; } = new();
}
