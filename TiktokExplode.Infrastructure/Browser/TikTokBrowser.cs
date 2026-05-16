using Microsoft.Playwright;
using TiktokExplode.Domain.Exceptions;

namespace TiktokExplode.Infrastructure.Browser;

internal sealed class TikTokBrowser : IAsyncDisposable
{
    private readonly IPlaywright _playwright;
    private readonly IBrowser _browser;
    private readonly IBrowserContext _context;

    private TikTokBrowser(IPlaywright playwright, IBrowser browser, IBrowserContext context)
    {
        _playwright = playwright;
        _browser = browser;
        _context = context;
    }

    public static async Task<TikTokBrowser> CreateAsync()
    {
        var playwright = await Playwright.CreateAsync();

        var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Channel = "msedge",
            Headless = true
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

        return new TikTokBrowser(playwright, browser, context);
    }

    private static readonly string[] _blockedResourceTypes = ["image", "media", "font", "stylesheet"];

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
                Timeout = 30_000
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

    /// <summary>
    /// Returns all cookies from the browser context with their domain and path.
    /// </summary>
    public async Task<IEnumerable<(string Name, string Value, string Domain, string Path)>> GetCookiesAsync()
    {
        var cookies = await _context.CookiesAsync();
        return cookies.Select(c => (c.Name, c.Value, c.Domain, c.Path ?? "/"));
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        await _browser.DisposeAsync();
        _playwright.Dispose();
    }
}
