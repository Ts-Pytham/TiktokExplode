using TiktokExplode.Domain.Abstractions;
using TiktokExplode.Domain.Entities;
using TiktokExplode.Domain.Exceptions;
using TiktokExplode.Domain.Utilities;
using TiktokExplode.Infrastructure.Browser;
using TiktokExplode.Infrastructure.Http;
using TiktokExplode.Infrastructure.Options;
using TiktokExplode.Infrastructure.Parsers;

namespace TiktokExplode.Infrastructure.Clients;

public sealed class TiktokClient(TikTokOptions options) : IVideoClient, IAsyncDisposable
{
    private readonly TikTokSession _session = new();
    private readonly TikTokVideoParser _parser = new();
    private TikTokBrowser? _browser;

    public TiktokClient() : this(new TikTokOptions()) { }

    public async Task<Video> GetVideoAsync(string url, CancellationToken cancellationToken = default)
    {
        TikTokUrlValidator.Validate(url);

        _browser ??= await TikTokBrowser.CreateAsync();

        TiktokWafException? wafException = null;
        for (int attempt = 0; attempt <= options.MaxWafRetries; attempt++)
        {
            try
            {
                var html = await _browser.GetVideoPageAsync(url, cancellationToken);
                var cookies = await _browser.GetCookiesAsync();
                _session.InjectCookies(cookies);
                return await _parser.ParseAsync(html);
            }
            catch (TiktokWafException e) when (attempt < options.MaxWafRetries)
            {
                wafException = e;
                await Task.Delay(options.RetryBaseDelay * (attempt + 1), cancellationToken);
            }
        }

        throw wafException!;
    }

    public Task<Stream> DownloadAsync(Video video, CancellationToken cancellationToken = default)
    {
        var url = video.Info.DownloadLinks.OriginalUrl;
        return _session.DownloadVideoAsync(url, cancellationToken);
    }

    public Task<Stream> DownloadWatermarkedAsync(Video video, CancellationToken cancellationToken = default)
    {
        var url = video.Info.DownloadLinks.WatermarkedUrl;
        return _session.DownloadVideoAsync(url, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
            await _browser.DisposeAsync();

        _session.Dispose();
    }
}