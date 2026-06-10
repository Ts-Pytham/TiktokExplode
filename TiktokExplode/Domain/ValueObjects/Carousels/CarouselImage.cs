namespace TiktokExplode.Domain.ValueObjects.Carousels;

public sealed class CarouselImage
{
    public uint Width { get; init; }
    public uint Height { get; init; }
    public IReadOnlyList<string> Urls { get; init; } = [];
}
