namespace TiktokExplode.Domain.ValueObjects.Carousels;

/// <summary>
/// Represents the content of a TikTok image carousel post, including all images and related metadata
/// </summary>
public sealed class CarouselPost
{
    /// <summary>
    /// The total number of images in the carousel. This indicates how many images are included in the post.
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// The index of the currently displayed image in the carousel. This is a 1-based index 
    /// indicating which image is currently being viewed by the user.
    /// </summary>
    public CarouselImage Cover { get; init; } = new();

    /// <summary>
    /// A list of all images included in the carousel post, along with their metadata such as dimensions and URLs.
    /// </summary>
    public IReadOnlyList<CarouselImage> Images { get; init; } = [];
}
