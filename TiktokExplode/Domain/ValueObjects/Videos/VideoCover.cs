namespace TiktokExplode.Domain.ValueObjects.Videos;

/// <summary>
/// Represents the cover images for a video, including both static and animated versions.
/// </summary>
/// <remarks>The StaticUrl property holds the URL for the static cover image, while the AnimatedUrl property holds
/// the URL for the animated cover image. Both properties are initialized to empty strings.</remarks>
public sealed record VideoCover
{
    /// <summary>
    /// Gets the URL for static resources used by the application.
    /// </summary>
    /// <remarks>This property is initialized to an empty string. It is intended to be set to a valid URL that
    /// points to static content, such as images, stylesheets, or scripts, that the application may serve.</remarks>
    public string StaticUrl { get; init; } = string.Empty;

    /// <summary>
    /// Gets the URL of the animated content associated with this instance.
    /// </summary>
    /// <remarks>This property is initialized to an empty string. It is intended to hold the URL for animated
    /// resources, such as animated thumbnails or previews, which can be used in user interfaces or other display
    /// contexts.</remarks>
    public string AnimatedUrl { get; init; } = string.Empty;
}
