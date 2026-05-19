using TiktokExplode.Infrastructure.Browser;
using TiktokExplode.Infrastructure.Options;

namespace TiktokExplode.Infrastructure.Fetchers;

public sealed class PlaywrightFetcher(PlaywrightFetcherOptions options) : IPageFetcher, IAsyncDisposable
{
    private TikTokBrowser? _browser;
    public PlaywrightFetcher() : this(new PlaywrightFetcherOptions()) { }

    public async Task<PageFetchResult> FetchPageAsync(string url, CancellationToken cancellationToken = default)
    {
        _browser ??= await TikTokBrowser.CreateAsync(options);

        var htmlContent = await _browser.GetVideoPageAsync(url, cancellationToken);
        var cookies = await _browser.GetCookiesAsync();

        return new PageFetchResult
        {
            HtmlContent = htmlContent,
            Cookies = cookies
        };
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
            _browser = null;
        }
    }
}
