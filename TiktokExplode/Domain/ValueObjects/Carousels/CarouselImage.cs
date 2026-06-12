namespace TiktokExplode.Domain.ValueObjects.Carousels;

/// <summary>
/// Represents an individual image within a TikTok carousel post, 
/// including its dimensions and available URLs for different resolutions.
/// </summary>
public sealed class CarouselImage
{
    /// <summary>
    /// The width of the image in pixels. This can be used to determine the aspect ratio.
    /// </summary>
    public uint Width { get; init; }

    /// <summary>
    /// The height of the image in pixels. This can be used to determine the aspect ratio.
    /// </summary>
    public uint Height { get; init; }

    /// <summary>
    /// A list of URLs for the image at different resolutions.
    /// </summary>
    public IReadOnlyList<string> Urls { get; init; } = [];
}
