namespace TiktokExplode.Domain.ValueObjects.Carousels;

/// <summary>
/// Represents the content of a TikTok image carousel post, including all images and related metadata
/// </summary>
/// <remarks>
/// Equality is structural, but <see cref="Images"/> is compared by reference because it is a
/// <see cref="IReadOnlyList{T}"/>. Two separately parsed carousels never compare as equal.
/// </remarks>
public sealed record CarouselPost
{
    /// <summary>
    /// The title of the carousel post, as displayed by TikTok. Empty when the post has none.
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// The image used as the cover of the carousel.
    /// </summary>
    public CarouselImage Cover { get; init; } = new();

    /// <summary>
    /// A list of all images included in the carousel post, along with their metadata such as dimensions and URLs.
    /// </summary>
    public IReadOnlyList<CarouselImage> Images { get; init; } = [];
}
