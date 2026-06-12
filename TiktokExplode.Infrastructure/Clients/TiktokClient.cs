using System.Diagnostics;
using TiktokExplode.Domain.Abstractions;
using TiktokExplode.Domain.Entities;
using TiktokExplode.Domain.Exceptions;
using TiktokExplode.Domain.Utilities;
using TiktokExplode.Domain.ValueObjects;
using TiktokExplode.Domain.ValueObjects.Carousels;
using TiktokExplode.Infrastructure.Fetchers;
using TiktokExplode.Infrastructure.Http;
using TiktokExplode.Infrastructure.Options;
using TiktokExplode.Infrastructure.Parsers;

namespace TiktokExplode.Infrastructure.Clients;

/// <summary>
/// Default implementation of <see cref="IVideoClient"/> that fetches and parses TikTok video pages,
/// then streams video files from TikTok's CDN.
/// Supports automatic WAF-retry logic configured via <see cref="TiktokOptions"/>.
/// </summary>
/// <param name="fetcher">The strategy used to fetch TikTok video page HTML.</param>
/// <param name="options">Retry and delay settings for WAF bypass attempts.</param>
public sealed class TiktokClient(IPageFetcher fetcher, TiktokOptions options) : IVideoClient, IAsyncDisposable
{
    /// <summary>Reusable HTTP client for CDN video downloads. Receives cookies from the fetcher.</summary>
    private readonly TiktokDownloadClient _downloadClient = new();

    /// <summary>Stateless parser that extracts a <see cref="Video"/> from raw page HTML.</summary>
    private readonly TiktokVideoParser _parser = new();

    /// <summary>
    /// Initializes a new <see cref="TiktokClient"/> with a Playwright-based page fetcher
    /// and default retry options.
    /// </summary>
    public TiktokClient() : this(new PlaywrightFetcher(), new TiktokOptions()) { }

    /// <inheritdoc/>
    /// <remarks>
    /// On each attempt the page is fetched, cookies are injected into the download client,
    /// and the HTML is parsed. If a <see cref="TiktokWafException"/> is thrown, the method
    /// waits <c>RetryBaseDelay × attempt</c> before retrying, up to <c>MaxWafRetries</c> times.
    /// </remarks>
    public async Task<Video> GetVideoAsync(string url, CancellationToken cancellationToken = default)
    {
        TiktokUrlValidator.Validate(url);

        for (int attempt = 0; attempt <= options.MaxWafRetries; attempt++)
        {
            try
            {
                var result = await fetcher.FetchPageAsync(url, cancellationToken);

                _downloadClient.InjectCookies(result.Cookies);
                return await _parser.ParseAsync(result.HtmlContent);
            }
            catch (TiktokException) when (attempt < options.MaxWafRetries)
            {
                await Task.Delay(options.RetryBaseDelay * (attempt + 1), cancellationToken);
            }
        }
        throw new UnreachableException();
    }

    /// <summary>
    /// Core download helper that delegates to <see cref="TiktokDownloadClient"/>.
    /// Exposed as <c>internal</c> to allow unit-testing without a full <see cref="Video"/> graph.
    /// </summary>
    /// <param name="url">The CDN URL to stream from.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    internal Task<StreamInfo> DownloadCoreAsync(
        string url,
        CancellationToken cancellationToken = default)
        => _downloadClient.DownloadAsync(url, cancellationToken);

    /// <inheritdoc/>
    public async Task<StreamInfo> DownloadAsync(Video video, CancellationToken cancellationToken = default)
    {
        if(string.IsNullOrEmpty(video.Info.DownloadLinks.OriginalUrl))
            throw new TiktokException("Video does not contain a valid original download URL.");

        return await DownloadCoreAsync(video.Info.DownloadLinks.OriginalUrl, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<StreamInfo> DownloadWatermarkedAsync(Video video, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(video.Info.DownloadLinks.WatermarkedUrl))
            throw new TiktokException("Video does not contain a valid watermarked download URL.");

        return await DownloadCoreAsync(video.Info.DownloadLinks.WatermarkedUrl, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<StreamInfo> DownloadImageAsync(CarouselImage image, CancellationToken cancellationToken = default)
    {
        var url = image.Urls.FirstOrDefault(u => !string.IsNullOrEmpty(u)) 
            ?? throw new TiktokException("Image does not contain a valid download URL.");

        return DownloadCoreAsync(url, cancellationToken);
    }

    /// <summary>
    /// Disposes the download client and the page fetcher (supporting both
    /// <see cref="IAsyncDisposable"/> and <see cref="IDisposable"/> fetchers).
    /// </summary>
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

    /// <summary>
    /// Creates a <see cref="TiktokClient"/> that uses a real Chromium browser (Playwright)
    /// to fetch pages, providing the best WAF bypass capability.
    /// </summary>
    /// <param name="browserOptions">Browser launch and navigation options. Uses defaults when <see langword="null"/>.</param>
    /// <param name="options">Retry and delay settings. Uses defaults when <see langword="null"/>.</param>
    public static TiktokClient CreateWithBrowser(PlaywrightFetcherOptions? browserOptions = null, TiktokOptions? options = null)
        => new(new PlaywrightFetcher(browserOptions ?? new PlaywrightFetcherOptions()), options ?? new TiktokOptions());

    /// <summary>
    /// Creates a <see cref="TiktokClient"/> that uses a plain <see cref="System.Net.Http.HttpClient"/>
    /// to fetch pages. Lighter than the browser strategy but more susceptible to WAF blocks.
    /// </summary>
    /// <param name="httpOptions">HTTP fetch options. Uses defaults when <see langword="null"/>.</param>
    /// <param name="options">Retry and delay settings. Uses defaults when <see langword="null"/>.</param>
    public static TiktokClient CreateWithHttp(HttpFetcherOptions? httpOptions = null, TiktokOptions? options = null)
        => new(new HttpFetcher(httpOptions ?? new HttpFetcherOptions()), options ?? new TiktokOptions());
}