using System.Net;
using TiktokExplode.Domain.Exceptions;
using TiktokExplode.Infrastructure.Options;

namespace TiktokExplode.Infrastructure.Fetchers;

/// <summary>
/// An <see cref="IPageFetcher"/> implementation that fetches TikTok pages using a plain
/// <see cref="HttpClient"/>. Before the first fetch it performs a one-time warmup request
/// to <c>tiktok.com</c> to populate session cookies.
/// This strategy is lightweight but may be blocked by TikTok's WAF on some networks.
/// </summary>
public sealed class HttpFetcher : IPageFetcher, IDisposable
{
    /// <summary>Options controlling warmup delay and user-agent.</summary>
    private readonly HttpFetcherOptions _options;

    /// <summary>Shared cookie container — populated during warmup and reused for all page requests.</summary>
    private readonly CookieContainer _cookies = new();

    /// <summary>Socket-level HTTP handler with connection pooling and automatic decompression.</summary>
    private readonly SocketsHttpHandler _handler;

    /// <summary>The configured HTTP client used for all page fetch requests.</summary>
    private readonly HttpClient _httpClient;

    /// <summary>Semaphore that serializes the one-time warmup request across concurrent callers.</summary>
    private readonly SemaphoreSlim _warmupLock = new(1, 1);

    /// <summary>
    /// Volatile flag indicating whether the warmup request has completed.
    /// Read outside the lock for a fast-path check.
    /// </summary>
    private volatile bool _warmedUp;

    /// <summary>
    /// Initializes a new <see cref="HttpFetcher"/> with the given <paramref name="options"/>.
    /// Configures the handler with connection pooling, automatic decompression, and cookie support.
    /// </summary>
    public HttpFetcher(HttpFetcherOptions options)
    {
        _options = options;

        _handler = new SocketsHttpHandler
        {
            CookieContainer = _cookies,
            UseCookies = true,

            AutomaticDecompression =
                DecompressionMethods.GZip |
                DecompressionMethods.Deflate |
                DecompressionMethods.Brotli,

            AllowAutoRedirect = true,

            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),

            MaxConnectionsPerServer = 10,

            EnableMultipleHttp2Connections = true,
        };

        _httpClient = new HttpClient(_handler);

        ConfigureDefaultHeaders();
    }

    /// <summary>Initializes a new <see cref="HttpFetcher"/> with default options.</summary>
    public HttpFetcher() : this(new HttpFetcherOptions()) { }

    /// <summary>
    /// Sets browser-like default request headers to reduce WAF detection probability.
    /// Uses <c>TryAddWithoutValidation</c> to allow non-standard header casing and values.
    /// </summary>
    private void ConfigureDefaultHeaders()
    {
        var headers = _httpClient.DefaultRequestHeaders;

        headers.Clear();

        headers.TryAddWithoutValidation("User-Agent", _options.UserAgent);
        headers.TryAddWithoutValidation("Accept", "*/*");
        headers.TryAddWithoutValidation("Cache-Control", "no-cache");
        headers.TryAddWithoutValidation("Connection", "keep-alive");
    }

    /// <summary>
    /// Fetches the HTML content of a TikTok video page.
    /// Performs a one-time warmup request first if not yet done.
    /// </summary>
    /// <param name="url">The absolute TikTok video URL to fetch.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A <see cref="PageFetchResult"/> with the page HTML and captured cookies.</returns>
    /// <exception cref="TiktokHttpException">Thrown when TikTok responds with a non-success status code.</exception>
    /// <exception cref="TiktokWafException">Thrown if WAF challenge markers are found in the response body.</exception>
    /// <exception cref="TiktokUnavailablePageException">
    /// Thrown when TikTok serves the branded placeholder page instead of the hydration payload.
    /// </exception>
    public async Task<PageFetchResult> FetchPageAsync(string url, CancellationToken cancellationToken = default)
    {
        await EnsureWarmedUpAsync(cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Get, url)
        {
            Version = HttpVersion.Version11,
            VersionPolicy = HttpVersionPolicy.RequestVersionExact
        };

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw TiktokHttpException.FromStatus((int)response.StatusCode, url);

        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (content.Contains("_wafchallengeid", StringComparison.OrdinalIgnoreCase))
            throw new TiktokWafException("TikTok WAF challenge detected.");

        if (!content.Contains("__UNIVERSAL_DATA_FOR_REHYDRATION__", StringComparison.OrdinalIgnoreCase))
            throw new TiktokUnavailablePageException(
                $"TikTok served a placeholder page for '{url}' without the hydration payload. The request was soft-blocked.");

        var cookies = _cookies.GetAllCookies()
            .Cast<Cookie>()
            .Select(c => new CookieData
            {
                Name = c.Name,
                Value = c.Value,
                Domain = c.Domain,
                Path = c.Path
            })
            .ToList();

        return new PageFetchResult
        {
            HtmlContent = content,
            Cookies = cookies
        };
    }

    /// <summary>
    /// Performs a one-time warmup GET request to <c>https://www.tiktok.com/</c> to populate
    /// session cookies and establish a connection. Uses a double-check locking pattern to ensure
    /// only one warmup is performed even when called concurrently.
    /// </summary>
    private async Task EnsureWarmedUpAsync(CancellationToken cancellationToken)
    {
        if (_warmedUp) return;

        await _warmupLock.WaitAsync(cancellationToken);
        try
        {
            if (_warmedUp) return;

            using var request = new HttpRequestMessage(HttpMethod.Get, "https://www.tiktok.com/")
            {
                Version = HttpVersion.Version11,
                VersionPolicy = HttpVersionPolicy.RequestVersionExact
            };

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            _ = await response.Content.ReadAsStringAsync(cancellationToken);

            await Task.Delay(_options.WarmupDelay, cancellationToken);

            _warmedUp = true;
        }
        finally
        {
            _warmupLock.Release();
        }
    }

    /// <summary>Disposes the HTTP client, handler, and semaphore.</summary>
    public void Dispose()
    {
        _httpClient.Dispose();
        _handler.Dispose();
        _warmupLock.Dispose();
    }
}
