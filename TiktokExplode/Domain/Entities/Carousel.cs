using TiktokExplode.Domain.ValueObjects.Carousels;

namespace TiktokExplode.Domain.Entities;

/// <summary>
/// Represents a TikTok image carousel post, which contains multiple images in a single post.
/// </summary>
public sealed class Carousel : Media
{
    /// <summary>
    /// The content of the carousel post, including all images and related metadata
    /// such as the total number of images, the index of the currently displayed image,
    /// and any captions or descriptions for each image.
    /// </summary>
    public CarouselPost Post { get; init; } = new();

    /// <summary>The URL of the carousel post on TikTok's website.</summary>
    public override string Url => $"https://www.tiktok.com/@{Author.UniqueId}/photo/{Id}";
}
