namespace TiktokExplode.Infrastructure.Fetchers;

/// <summary>
/// Represents a single HTTP cookie captured from a TikTok page response.
/// Used to carry session state from the page fetcher to the download client.
/// </summary>
public sealed record CookieData
{
    /// <summary>The name of the cookie.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>The value of the cookie.</summary>
    public string Value { get; init; } = string.Empty;

    /// <summary>The domain scope of the cookie (e.g. <c>.tiktok.com</c>).</summary>
    public string Domain { get; init; } = string.Empty;

    /// <summary>The path scope of the cookie (e.g. <c>/</c>).</summary>
    public string Path { get; init; } = string.Empty;
}
