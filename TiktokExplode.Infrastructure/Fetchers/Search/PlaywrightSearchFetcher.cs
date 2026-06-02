using System.Text.Json.Nodes;
using TiktokExplode.Infrastructure.Browser;
using TiktokExplode.Infrastructure.Options;

namespace TiktokExplode.Infrastructure.Fetchers.Search;

public sealed class PlaywrightSearchFetcher(PlaywrightFetcherOptions options) 
    : ISearchFetcher, IAsyncDisposable
{

    private TikTokBrowser? _browser;

    /// <summary>Semaphore that serializes the one-time browser initialization across concurrent callers.</summary>
    private readonly SemaphoreSlim _initLock = new(1, 1);

    /// <summary>
    /// Volatile flag indicating whether <see cref="_browser"/> has been fully initialized.
    /// Read outside the lock for a fast-path check; written inside the lock after init completes.
    /// </summary>
    private volatile bool _initialized = false;

    public PlaywrightSearchFetcher() : this(new PlaywrightFetcherOptions()) { }

    public async IAsyncEnumerable<SearchFetchResult> FetchSearchAsync(
        string keyword, 
        CancellationToken cancellationToken = default)
    {
        if (!_initialized)
        {
            await _initLock.WaitAsync(cancellationToken);
            try
            {
                if (!_initialized)
                {
                    _browser ??= await TikTokBrowser.CreateAsync(options);
                    _initialized = true;
                }
            }
            finally
            {
                _initLock.Release();
            }
        }

        var json = await _browser!.GetSearchPageAsync(keyword);
        var cookies = await _browser.GetCookiesAsync();

        yield return new SearchFetchResult { JsonContent = json, Cookies = cookies };

        var root = JsonNode.Parse(json);
        var hasMore = root?["has_more"]?.GetValue<int>() ?? 0;
        var cursor = root?["cursor"]?.GetValue<long>() ?? 0;

        var page = await _browser.CreatePageAsync();

        while(hasMore == 1 && !cancellationToken.IsCancellationRequested)
        {
            var nextJson = await _browser.GetSearchNextPageAsync(keyword, cursor, page);
            yield return new SearchFetchResult { JsonContent = nextJson, Cookies = cookies };

            var nextRoot = JsonNode.Parse(nextJson);
            hasMore = nextRoot?["has_more"]?.GetValue<int>() ?? 0;
            cursor = nextRoot?["cursor"]?.GetValue<long>() ?? 0;
        }

        await page.CloseAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
            _browser = null;
        }
        _initLock.Dispose();
    }
}
