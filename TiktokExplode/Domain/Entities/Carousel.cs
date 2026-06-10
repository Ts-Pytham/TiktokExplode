using TiktokExplode.Domain.ValueObjects.Carousels;

namespace TiktokExplode.Domain.Entities;

public sealed class Carousel : Media
{
    public CarouselPost Post { get; init; }
}
