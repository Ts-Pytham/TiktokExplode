namespace TiktokExplode.Domain.ValueObjects.Carousels;

public sealed class CarouselPost
{
    public string Title { get; init; } = string.Empty;
    public CarouselImage Cover { get; init; }
    public IReadOnlyList<CarouselImage> Images { get; init; } = [];
}
