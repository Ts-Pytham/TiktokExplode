namespace TiktokExplode.Infrastructure.Fetchers.Search;

/// <summary>
/// Defines the strategy for fetching paginated TikTok search results by keyword.
/// Implementations may use a real browser (Playwright) or a plain HTTP client.
/// </summary>
public interface ISearchFetcher
{
    /// <summary>
    /// Asynchronously streams paginated search result pages for the given <paramref name="keyword"/>.
    /// Each yielded <see cref="SearchFetchResult"/> contains the raw JSON for one page
    /// together with the browser cookies needed to authenticate CDN downloads.
    /// </summary>
    /// <param name="keyword">The search term to query on TikTok.</param>
    /// <param name="cancellationToken">Token to cancel the enumeration.</param>
    /// <returns>
    /// An <see cref="IAsyncEnumerable{T}"/> of <see cref="SearchFetchResult"/> values,
    /// one per API page, until no further results are available.
    /// </returns>
    IAsyncEnumerable<SearchFetchResult> FetchSearchAsync(string keyword, CancellationToken cancellationToken = default);
}
