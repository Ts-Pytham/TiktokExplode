namespace TiktokExplode.Domain.ValueObjects.Media;

/// <summary>
/// Represents the language properties detected for a TikTok media's text content.
/// </summary>
public sealed record MediaLanguage
{
    /// <summary>
    /// The BCP-47 language code of the primary language detected in the media
    /// (e.g. <c>"en"</c>, <c>"es"</c>). Empty string if not detected.
    /// </summary>
    public string PrimaryLanguage { get; init; } = string.Empty;

    /// <summary>
    /// <see langword="true"/> if TikTok considers the media's text eligible
    /// for automatic translation; otherwise <see langword="false"/>.
    /// </summary>
    public bool IsTranslatable { get; init; }
}
