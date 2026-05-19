namespace TiktokExplode.Domain.ValueObjects.Authors;

/// <summary>
/// Aggregated social statistics for a TikTok author's account.
/// </summary>
public sealed record AuthorStats
{
    /// <summary>Total number of accounts following this author.</summary>
    public long Followers { get; init; }

    /// <summary>Total number of accounts this author follows.</summary>
    public long Following { get; init; }

    /// <summary>Number of mutual-follow (friend) connections this author has.</summary>
    public long Friends { get; init; }

    /// <summary>Total number of likes received across all of this author's videos.</summary>
    public long LikesReceived { get; init; }

    /// <summary>Total number of videos published by this author.</summary>
    public long VideoCount { get; init; }
}
