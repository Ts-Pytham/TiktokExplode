namespace TiktokExplode.Domain.ValueObjects.Authors;

/// <summary>
/// Holds URLs for an author's avatar image in three TikTok-provided size variants.
/// </summary>
public sealed record ProfileImageVariants
{
    /// <summary>URL to the large-resolution avatar image.</summary>
    public string Larger { get; init; } = string.Empty;

    /// <summary>URL to the medium-resolution avatar image.</summary>
    public string Medium { get; init; } = string.Empty;

    /// <summary>URL to the small (thumbnail) avatar image.</summary>
    public string Small { get; init; } = string.Empty;
}
