namespace TiktokExplode.Domain.ValueObjects.Videos;

/// <summary>
/// Represents the language properties detected for a TikTok video's text content.
/// </summary>
public sealed record VideoLanguage
{
    /// <summary>
    /// The BCP-47 language code of the primary language detected in the video
    /// (e.g. <c>"en"</c>, <c>"es"</c>). Empty string if not detected.
    /// </summary>
    public string PrimaryLanguage { get; init; } = string.Empty;

    /// <summary>
    /// <see langword="true"/> if TikTok considers the video's text eligible
    /// for automatic translation; otherwise <see langword="false"/>.
    /// </summary>
    public bool IsTranslatable { get; init; }
}
