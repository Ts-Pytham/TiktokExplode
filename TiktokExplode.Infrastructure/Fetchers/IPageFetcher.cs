namespace TiktokExplode.Infrastructure.Fetchers;

public interface IPageFetcher
{
    Task<PageFetchResult> FetchPageAsync(string url, CancellationToken cancellationToken = default);
}
