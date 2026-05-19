using System.Diagnostics;
using TiktokExplode.Domain.Abstractions;
using TiktokExplode.Domain.Entities;
using TiktokExplode.Domain.Exceptions;
using TiktokExplode.Domain.Utilities;
using TiktokExplode.Domain.ValueObjects;
using TiktokExplode.Infrastructure.Fetchers;
using TiktokExplode.Infrastructure.Http;
using TiktokExplode.Infrastructure.Options;
using TiktokExplode.Infrastructure.Parsers;

namespace TiktokExplode.Infrastructure.Clients;

public sealed class TiktokClient(IPageFetcher fetcher, TikTokOptions options) : IVideoClient, IAsyncDisposable
{
    private readonly TikTokDownloadClient _downloadClient = new();
    private readonly TikTokVideoParser _parser = new();

    public TiktokClient() : this(new PlaywrightFetcher(), new TikTokOptions()) { }

    public async Task<Video> GetVideoAsync(string url, CancellationToken cancellationToken = default)
    {
        TikTokUrlValidator.Validate(url);

        for (int attempt = 0; attempt <= options.MaxWafRetries; attempt++)
        {
            try
            {
                var result = await fetcher.FetchPageAsync(url, cancellationToken);

                _downloadClient.InjectCookies(result.Cookies);
                return await _parser.ParseAsync(result.HtmlContent);
            }
            catch (TiktokWafException) when (attempt < options.MaxWafRetries)
            {
                await Task.Delay(options.RetryBaseDelay * (attempt + 1), cancellationToken);
            }
        }
        throw new UnreachableException();
    }

    internal Task<StreamInfo> DownloadCoreAsync(
        string url,
        CancellationToken cancellationToken = default)
        => _downloadClient.DownloadVideoAsync(url, cancellationToken);

    public async Task<StreamInfo> DownloadAsync(Video video, CancellationToken cancellationToken = default)
    {
        return await DownloadCoreAsync(video.Info.DownloadLinks.OriginalUrl, cancellationToken);
    }

    public async Task<StreamInfo> DownloadWatermarkedAsync(Video video, CancellationToken cancellationToken = default)
    {
        return await DownloadCoreAsync(video.Info.DownloadLinks.WatermarkedUrl, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        _downloadClient.Dispose();

        if (fetcher is IAsyncDisposable asyncDisposableFetcher)
        {
            await asyncDisposableFetcher.DisposeAsync();
        }
        else if (fetcher is IDisposable disposableFetcher)
        {
            disposableFetcher.Dispose();
        }
    }

    public static TiktokClient CreateWithBrowser(PlaywrightFetcherOptions? browserOptions = null, TikTokOptions? options = null)
        => new(new PlaywrightFetcher(browserOptions ?? new PlaywrightFetcherOptions()), options ?? new TikTokOptions());

    public static TiktokClient CreateWithHttp(HttpFetcherOptions? httpOptions = null, TikTokOptions? options = null)
        => new(new HttpFetcher(httpOptions ?? new HttpFetcherOptions()), options ?? new TikTokOptions());
}