using System.Net;
using TiktokExplode.Domain.Exceptions;
using TiktokExplode.Infrastructure.Options;

namespace TiktokExplode.Infrastructure.Fetchers;

public sealed class HttpFetcher : IPageFetcher, IDisposable
{
    private readonly HttpFetcherOptions _options;
    private readonly CookieContainer _cookies = new();
    private readonly SocketsHttpHandler _handler;
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _warmupLock = new(1, 1);
    private volatile bool _warmedUp;

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

            EnableMultipleHttp2Connections = true
        };

        _httpClient = new HttpClient(_handler);

        ConfigureDefaultHeaders();
    }

    public HttpFetcher() : this(new HttpFetcherOptions()) { }

    private void ConfigureDefaultHeaders()
    {
        var headers = _httpClient.DefaultRequestHeaders;

        headers.Clear();

        headers.TryAddWithoutValidation("User-Agent",                _options.UserAgent);
        headers.TryAddWithoutValidation("Accept",                    "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8");
        headers.TryAddWithoutValidation("Accept-Language",           "en-US,en;q=0.9");
        headers.TryAddWithoutValidation("Accept-Encoding",           "gzip, deflate, br");
        headers.TryAddWithoutValidation("Upgrade-Insecure-Requests", "1");
        headers.TryAddWithoutValidation("Sec-Fetch-Dest",            "document");
        headers.TryAddWithoutValidation("Sec-Fetch-Mode",            "navigate");
        headers.TryAddWithoutValidation("Sec-Fetch-Site",            "none");
        headers.TryAddWithoutValidation("Sec-Fetch-User",            "?1");
        headers.TryAddWithoutValidation("sec-ch-ua",                 "\"Chromium\";v=\"136\", \"Google Chrome\";v=\"136\", \"Not.A/Brand\";v=\"99\"");
        headers.TryAddWithoutValidation("sec-ch-ua-mobile",          "?0");
        headers.TryAddWithoutValidation("sec-ch-ua-platform",        "\"Windows\"");
    }

    public async Task<PageFetchResult> FetchPageAsync(string url, CancellationToken cancellationToken = default)
    {
        await EnsureWarmedUpAsync(cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Get, url)
        {
            Version = HttpVersion.Version20,
            VersionPolicy = HttpVersionPolicy.RequestVersionOrLower
        };

        request.Headers.Referrer = new Uri("https://www.tiktok.com/");

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (content.Contains("_wafchallengeid", StringComparison.OrdinalIgnoreCase))
            throw new TiktokWafException("TikTok WAF challenge detected.");

        var cookies = _cookies.GetAllCookies()
            .Cast<Cookie>()
            .Select(c => new CookieData
            {
                Name   = c.Name,
                Value  = c.Value,
                Domain = c.Domain,
                Path   = c.Path
            })
            .ToList();

        return new PageFetchResult
        {
            HtmlContent = content,
            Cookies     = cookies
        };
    }

    private async Task EnsureWarmedUpAsync(CancellationToken cancellationToken)
    {
        if (_warmedUp) return;

        await _warmupLock.WaitAsync(cancellationToken);
        try
        {
            if (_warmedUp) return;

            using var request = new HttpRequestMessage(HttpMethod.Get, "https://www.tiktok.com/")
            {
                Version = HttpVersion.Version20,
                VersionPolicy = HttpVersionPolicy.RequestVersionOrLower
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

    public void Dispose()
    {
        _httpClient.Dispose();
        _handler.Dispose();
        _warmupLock.Dispose();
    }
}
