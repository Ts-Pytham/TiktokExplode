namespace TiktokExplode.Domain.ValueObjects.Authors;

public sealed record ProfileImageVariants
{
    public string Larger { get; init; } = string.Empty;
    public string Medium { get; init; } = string.Empty;
    public string Small { get; init; } = string.Empty;
}
