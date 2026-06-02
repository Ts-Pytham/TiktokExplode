using TiktokExplode.Infrastructure.Browser;
using TiktokExplode.Infrastructure.Options;

namespace TiktokExplode.Infrastructure.Fetchers;

/// <summary>
/// An <see cref="IPageFetcher"/> implementation that uses a real Chromium browser via
/// Microsoft Playwright to load TikTok pages. This is the most reliable strategy because
/// it executes JavaScript, handles browser fingerprinting, and can bypass TikTok's WAF.
/// </summary>
public sealed class PlaywrightFetcher(PlaywrightFetcherOptions options) : IPageFetcher, IAsyncDisposable
{
    /// <summary>
    /// The underlying browser wrapper. Lazily initialized on the first
    /// <see cref="FetchPageAsync"/> call via the double-check lock pattern.
    /// </summary>
    private TiktokBrowser? _browser;

    /// <summary>Semaphore that serializes the one-time browser initialization across concurrent callers.</summary>
    private readonly SemaphoreSlim _initLock = new(1, 1);

    /// <summary>
    /// Volatile flag indicating whether <see cref="_browser"/> has been fully initialized.
    /// Read outside the lock for a fast-path check; written inside the lock after init completes.
    /// </summary>
    private volatile bool _initialized;

    /// <summary>Initializes a new <see cref="PlaywrightFetcher"/> with default options.</summary>
    public PlaywrightFetcher() : this(new PlaywrightFetcherOptions()) { }

    /// <summary>
    /// Fetches the HTML content of a TikTok video page using a real browser session.
    /// The browser is initialized lazily on the first call using a thread-safe double-check pattern.
    /// </summary>
    /// <param name="url">The absolute TikTok video URL to navigate to.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A <see cref="PageFetchResult"/> containing the page HTML and current browser cookies.</returns>
    public async Task<PageFetchResult> FetchPageAsync(string url, CancellationToken cancellationToken = default)
    {
        if (!_initialized)
        {
            await _initLock.WaitAsync(cancellationToken);
            try
            {
                if (!_initialized)
                {
                    _browser = await TiktokBrowser.CreateAsync(options);
                    _initialized = true;
                }
            }
            finally
            {
                _initLock.Release();
            }
        }

        var htmlContent = await _browser!.GetVideoPageAsync(url, cancellationToken);
        var cookies = await _browser.GetCookiesAsync();

        return new PageFetchResult
        {
            HtmlContent = htmlContent,
            Cookies = cookies
        };
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
