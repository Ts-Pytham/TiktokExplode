namespace TiktokExplode.Domain.ValueObjects.Videos;

public sealed record VideoStats
{
    public long Views { get; init; }
    public long Likes { get; init; }
    public long Comments { get; init; }
    public long Shares { get; init; }
    public long Favorites { get; init; }
    public long Reposts { get; init; }
}
