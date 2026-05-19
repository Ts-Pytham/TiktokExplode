namespace TiktokExplode.Infrastructure.Fetchers;

/// <summary>
/// Carries the result of a single <see cref="IPageFetcher.FetchPageAsync"/> call:
/// the raw HTML of the TikTok video page and the session cookies captured during the request.
/// </summary>
public sealed record PageFetchResult
{
    /// <summary>The full HTML source of the fetched TikTok video page.</summary>
    public required string HtmlContent { get; init; }

    /// <summary>
    /// Session cookies captured during the page fetch.
    /// These are injected into the download client to authenticate CDN video requests.
    /// </summary>
    public required IReadOnlyList<CookieData> Cookies { get; init; }
}