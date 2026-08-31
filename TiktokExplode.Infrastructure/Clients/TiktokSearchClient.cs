using System.Runtime.CompilerServices;
using TiktokExplode.Domain.Abstractions;
using TiktokExplode.Domain.Entities;
using TiktokExplode.Domain.Exceptions;
using TiktokExplode.Domain.ValueObjects;
using TiktokExplode.Domain.ValueObjects.Carousels;
using TiktokExplode.Infrastructure.Fetchers.Search;
using TiktokExplode.Infrastructure.Http;
using TiktokExplode.Infrastructure.Options;
using TiktokExplode.Infrastructure.Parsers;

namespace TiktokExplode.Infrastructure.Clients;

/// <summary>
/// An <see cref="ISearchClient"/> implementation that searches TikTok by keyword using
/// a Playwright-powered browser fetcher and streams the results as <see cref="Video"/> objects.
/// </summary>
public sealed class TiktokSearchClient(
    ISearchFetcher fetcher) : ISearchClient
{
    private readonly TiktokDownloadClient _downloadClient = new();

    /// <summary>Initializes a new <see cref="TiktokSearchClient"/> with default options.</summary>
    public TiktokSearchClient() : this(new PlaywrightSearchFetcher()) { }

    internal TiktokSearchClient(
        TiktokDownloadClient downloadClient,
        ISearchFetcher fetcher) : this(fetcher)
    {
        _downloadClient = downloadClient;
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<Media> SearchAsync(
        string keyword,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyword);

        await foreach (var result in fetcher.FetchSearchAsync(keyword, cancellationToken))
        {
            _downloadClient.InjectCookies(result.Cookies);

            foreach (var video in TiktokSearchParser.Parse(result.JsonContent))
            {
                yield return video;
            }
        }
    }

    /// <inheritdoc/>
    public async Task<StreamInfo> DownloadAsync(Video video, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(video.Info.DownloadLinks.OriginalUrl))
            throw new TiktokException("Original download URL is not available for this video.");

        return await DownloadCoreAsync(video.Info.DownloadLinks.OriginalUrl, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<StreamInfo> DownloadWatermarkedAsync(Video video, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(video.Info.DownloadLinks.WatermarkedUrl))
            throw new TiktokException("Watermarked download URL is not available for this video.");

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
    public async ValueTask DisposeAsync()
    {
        _downloadClient.Dispose();
        if (fetcher is IAsyncDisposable d) await d.DisposeAsync();
        else if (fetcher is IDisposable d2) d2.Dispose();
    }

    /// <summary>
    /// Factory method to create a <see cref="TiktokSearchClient"/> with a Playwright-based fetcher.
    /// </summary>
    /// <param name="browserOptions">Options for the Playwright browser.</param>
    /// <param name="options">Options for the TikTok client.</param>
    /// <returns>A new instance of <see cref="TiktokSearchClient"/>.</returns>
    public static TiktokSearchClient CreateWithBrowser(
        PlaywrightFetcherOptions? browserOptions = null,
        TiktokOptions? options = null)
        => new(new PlaywrightSearchFetcher(
            browserOptions ?? new PlaywrightFetcherOptions(),
            options ?? new TiktokOptions()));
}
