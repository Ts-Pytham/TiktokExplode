namespace TiktokExplode.Infrastructure.Fetchers.Search;

public interface ISearchFetcher
{
    IAsyncEnumerable<SearchFetchResult> FetchSearchAsync(string keyword, CancellationToken cancellationToken = default);
}
