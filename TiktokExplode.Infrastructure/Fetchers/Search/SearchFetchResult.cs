namespace TiktokExplode.Infrastructure.Fetchers.Search;

public sealed record SearchFetchResult
{
    /// <summary>The raw JSON content of the search results returned by TikTok's search API.</summary>
    public required string JsonContent { get; init; }

    /// <summary>
    /// Session cookies captured during the page fetch.
    /// These are injected into the download client to authenticate CDN video requests.
    /// </summary>
    public required IReadOnlyList<CookieData> Cookies { get; init; }
}
