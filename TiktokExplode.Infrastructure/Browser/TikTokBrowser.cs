using Microsoft.Playwright;
using TiktokExplode.Domain.Exceptions;
using TiktokExplode.Infrastructure.Fetchers;
using TiktokExplode.Infrastructure.Options;

namespace TiktokExplode.Infrastructure.Browser;

/// <summary>
/// Internal wrapper around a Playwright browser session configured to load TikTok video pages.
/// Manages a single <see cref="IBrowserContext"/> shared across multiple page navigations.
/// Resource-heavy assets (images, media, fonts, stylesheets) are intercepted and aborted
/// to speed up page load times.
/// </summary>
internal sealed class TiktokBrowser : IAsyncDisposable
{
    /// <summary>The top-level Playwright instance. Must be disposed last.</summary>
    private readonly IPlaywright _playwright;

    /// <summary>The launched Chromium browser process.</summary>
    private readonly IBrowser _browser;

    /// <summary>
    /// The browser context (equivalent to an incognito profile) shared across all page navigations.
    /// </summary>
    private readonly IBrowserContext _context;

    /// <summary>Browser launch and navigation options.</summary>
    private readonly PlaywrightFetcherOptions _options;

    /// <summary>Private constructor — use <see cref="CreateAsync"/> to instantiate.</summary>
    private TiktokBrowser(
        IPlaywright playwright,
        IBrowser browser,
        IBrowserContext context,
        PlaywrightFetcherOptions options)
    {
        _playwright = playwright;
        _browser = browser;
        _context = context;
        _options = options;
    }

    /// <summary>
    /// Creates and fully initializes a new <see cref="TiktokBrowser"/> instance.
    /// Launches Chromium with the settings from <paramref name="options"/> and creates
    /// a new browser context with a realistic user-agent and locale.
    /// </summary>
    /// <param name="options">Browser launch and navigation options.</param>
    public static async Task<TiktokBrowser> CreateAsync(PlaywrightFetcherOptions options)
    {
        var playwright = await Playwright.CreateAsync();

        var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Channel = options.BrowserChannel,
            Headless = options.Headless
        });

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36 Edg/136.0.0.0",
            Locale = "en-US",
            ExtraHTTPHeaders = new Dictionary<string, string>
            {
                ["Accept-Language"] = "en-US,en;q=0.9"
            }
        });

        return new TiktokBrowser(playwright, browser, context, options);
    }

    /// <summary>
    /// Resource types that are aborted to speed up page load times.
    /// Images, media, fonts, and stylesheets are not needed to extract the JSON hydration data.
    /// </summary>
    private static readonly string[] _blockedResourceTypes = ["image", "media", "font", "stylesheet"];

    /// <summary>
    /// Opens a new page in the shared context, navigates to <paramref name="url"/>,
    /// waits for DOM content to load, and returns the full HTML source.
    /// The page is closed after content is extracted regardless of success or failure.
    /// </summary>
    /// <param name="url">The TikTok video URL to navigate to.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The full HTML content of the loaded page.</returns>
    /// <exception cref="TiktokParsingException">Thrown if the page fails to load (non-OK HTTP status).</exception>
    /// <exception cref="TiktokWafException">Thrown if WAF challenge markers are found in the page content.</exception>
    public async Task<string> GetVideoPageAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        var page = await _context.NewPageAsync();

        try
        {
            await page.RouteAsync("**/*", async route =>
            {
                if (_blockedResourceTypes.Contains(route.Request.ResourceType))
                    await route.AbortAsync();
                else
                    await route.ContinueAsync();
            });

            var response = await page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = _options.PageTimeoutMs
            });

            if (response is null || !response.Ok)
                throw new TiktokParsingException($"Failed to load page. Status: {response?.Status}");

            var content = await page.ContentAsync();

            if (content.Contains("_wafchallengeid", StringComparison.OrdinalIgnoreCase))
                throw new TiktokWafException("TikTok WAF challenge detected.");

            return content;
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    public async Task<string> GetSearchPageAsync(string keyword)
    {
        var page = await _context.NewPageAsync();

        try
        {
            var responseTask = page.WaitForResponseAsync(
               r => r.Url.Contains("/api/search/general/full/"),
               new PageWaitForResponseOptions { Timeout = _options.PageTimeoutMs });

            var pageResponse = await page.GotoAsync(
                $"https://www.tiktok.com/search?q={Uri.EscapeDataString(keyword)}", new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = _options.PageTimeoutMs
                });

            if (pageResponse is null || !pageResponse.Ok)
                throw new TiktokParsingException($"Failed to load page. Status: {pageResponse?.Status}");

            var content = await page.ContentAsync();

            if (content.Contains("_wafchallengeid", StringComparison.OrdinalIgnoreCase))
                throw new TiktokWafException("TikTok WAF challenge detected.");

            IResponse response;
            try
            {
                response = await responseTask;
            }
            catch (TimeoutException)
            {
                throw new TiktokParsingException("Search API request was not intercepted within the timeout period. TikTok may not have issued the search request.");
            }

            if (!response.Ok)
                throw new TiktokParsingException($"Failed to load search results. Status: {response.Status}");

            return await response.TextAsync();
        }
        finally
        {
            await page.CloseAsync();
        }

    }

    public async Task<string> GetSearchNextPageAsync(
        string keyword,
        long cursor,
        IPage page)
    {
        return await page.EvaluateAsync<string>("""
        async (args) => {
            const url = new URL('/api/search/general/full/', 'https://www.tiktok.com');
            url.searchParams.set('keyword', args.keyword);
            url.searchParams.set('cursor', args.cursor);
            url.searchParams.set('offset', args.cursor);
            url.searchParams.set('count', '12');
            url.searchParams.set('from_page', 'search');
            const res = await fetch(url.toString(), { credentials: 'include' });
            return await res.text();
        }
        """, new { keyword, cursor });
    }

    /// <summary>
    /// Returns all cookies currently set in the browser context as a list of
    /// <see cref="CookieData"/> records, ready to be injected into the download client.
    /// </summary>
    public async Task<IReadOnlyList<CookieData>> GetCookiesAsync()
    {
        var cookies = await _context.CookiesAsync();
        return [.. cookies
            .Select(c => new CookieData
            {
                Name   = c.Name,
                Value  = c.Value,
                Domain = c.Domain,
                Path   = c.Path ?? "/"
            })];
    }

    internal async Task<IPage> CreatePageAsync()
        => await _context.NewPageAsync();

    /// <summary>
    /// Disposes the browser context, the browser process, and the Playwright instance
    /// in the correct reverse-dependency order.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        await _browser.DisposeAsync();
        _playwright.Dispose();
    }
}
