namespace TiktokExplode.Infrastructure.Fetchers;

/// <summary>
/// Defines the strategy for fetching a TikTok video page and returning its raw HTML content
/// together with the session cookies set during the request.
/// Implement this interface to provide a custom page-fetching mechanism.
/// </summary>
public interface IPageFetcher
{
    /// <summary>
    /// Fetches the HTML content of a TikTok video page at the given <paramref name="url"/>.
    /// </summary>
    /// <param name="url">The absolute TikTok video URL to fetch.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A <see cref="PageFetchResult"/> containing the raw HTML and session cookies.</returns>
    /// <exception cref="Domain.Exceptions.TiktokWafException">
    /// Thrown when WAF protection is detected in the page response.
    /// </exception>
    Task<PageFetchResult> FetchPageAsync(string url, CancellationToken cancellationToken = default);
}
