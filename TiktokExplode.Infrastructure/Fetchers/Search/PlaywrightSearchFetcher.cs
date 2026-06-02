using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using Microsoft.Playwright;
using TiktokExplode.Domain.Exceptions;
using TiktokExplode.Infrastructure.Browser;
using TiktokExplode.Infrastructure.Options;

namespace TiktokExplode.Infrastructure.Fetchers.Search;

/// <summary>
/// An <see cref="ISearchFetcher"/> implementation that uses a real Chromium browser via
/// Microsoft Playwright to search TikTok. Intercepts the internal search API response on
/// the first page, then fetches subsequent pages via in-browser <c>fetch()</c> calls.
/// </summary>
public sealed class PlaywrightSearchFetcher(PlaywrightFetcherOptions options, TikTokOptions tikTokOptions)
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

    /// <summary>Initializes a new <see cref="PlaywrightSearchFetcher"/> with default options.</summary>
    public PlaywrightSearchFetcher() : this(new PlaywrightFetcherOptions(), new TikTokOptions()) { }

    /// <summary>
    /// Asynchronously streams pages of raw search results for <paramref name="keyword"/>.
    /// The browser is initialized lazily on the first call. WAF challenges are retried
    /// up to <see cref="TikTokOptions.MaxWafRetries"/> times with linear back-off.
    /// </summary>
    /// <param name="keyword">The search term to query.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>An async sequence of <see cref="SearchFetchResult"/> — one per API page.</returns>
    public async IAsyncEnumerable<SearchFetchResult> FetchSearchAsync(
        string keyword,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
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

        var json = await FetchFirstPageWithRetryAsync(keyword, cancellationToken);
        var cookies = await _browser!.GetCookiesAsync();

        yield return new SearchFetchResult { JsonContent = json, Cookies = cookies };

        var root = JsonNode.Parse(json);
        var hasMore = root?["has_more"]?.GetValue<int>() ?? 0;
        var cursor = root?["cursor"]?.GetValue<long>() ?? 0;

        var page = await _browser.CreatePageAsync();

        while (hasMore == 1 && !cancellationToken.IsCancellationRequested)
        {
            var nextJson = await FetchNextPageWithRetryAsync(keyword, cursor, page, cancellationToken);
            yield return new SearchFetchResult { JsonContent = nextJson, Cookies = cookies };

            var nextRoot = JsonNode.Parse(nextJson);
            hasMore = nextRoot?["has_more"]?.GetValue<int>() ?? 0;
            cursor = nextRoot?["cursor"]?.GetValue<long>() ?? 0;
        }

        await page.CloseAsync();
    }

    private async Task<string> FetchFirstPageWithRetryAsync(string keyword, CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt <= tikTokOptions.MaxWafRetries; attempt++)
        {
            try
            {
                return await _browser!.GetSearchPageAsync(keyword);
            }
            catch (TiktokWafException) when (attempt < tikTokOptions.MaxWafRetries)
            {
                await Task.Delay(tikTokOptions.RetryBaseDelay * (attempt + 1), cancellationToken);
            }
        }
        throw new UnreachableException();
    }

    private async Task<string> FetchNextPageWithRetryAsync(
        string keyword,
        long cursor,
        IPage page,
        CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt <= tikTokOptions.MaxWafRetries; attempt++)
        {
            try
            {
                return await _browser!.GetSearchNextPageAsync(keyword, cursor, page);
            }
            catch (TiktokWafException) when (attempt < tikTokOptions.MaxWafRetries)
            {
                await Task.Delay(tikTokOptions.RetryBaseDelay * (attempt + 1), cancellationToken);
            }
        }
        throw new UnreachableException();
    }

    /// <summary>
    /// Disposes the underlying browser and Playwright instance,
    /// and releases the initialization semaphore.
    /// </summary>
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
