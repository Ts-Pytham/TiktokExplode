namespace TiktokExplode.Domain.ValueObjects.Authors;

public sealed record AuthorStats
{
    public long Followers { get; init; }
    public long Following { get; init; }
    public long Friends { get; init; }
    public long LikesReceived { get; init; }
    public long VideoCount { get; init; }
}
