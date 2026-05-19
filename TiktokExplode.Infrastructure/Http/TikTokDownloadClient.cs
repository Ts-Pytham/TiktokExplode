using System.Net;
using TiktokExplode.Domain.ValueObjects;
using TiktokExplode.Infrastructure.Fetchers;

namespace TiktokExplode.Infrastructure.Http;

public sealed class TikTokDownloadClient : IDisposable
{
    private readonly CookieContainer _cookies = new();

    private readonly SocketsHttpHandler _handler;

    private readonly HttpClient _httpClient;

    public TikTokDownloadClient()
    {
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

    private void ConfigureDefaultHeaders()
    {
        var headers = _httpClient.DefaultRequestHeaders;

        headers.Clear();

        headers.TryAddWithoutValidation(
            "User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36");

        headers.TryAddWithoutValidation(
            "Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8");

        headers.TryAddWithoutValidation(
            "Accept-Language",
            "en-US,en;q=0.9");

        headers.TryAddWithoutValidation(
            "Accept-Encoding",
            "gzip, deflate, br");

        headers.TryAddWithoutValidation(
            "Upgrade-Insecure-Requests",
            "1");

        headers.TryAddWithoutValidation(
            "Sec-Fetch-Dest",
            "document");

        headers.TryAddWithoutValidation(
            "Sec-Fetch-Mode",
            "navigate");

        headers.TryAddWithoutValidation(
            "Sec-Fetch-Site",
            "none");

        headers.TryAddWithoutValidation(
            "Sec-Fetch-User",
            "?1");

        headers.TryAddWithoutValidation(
            "sec-ch-ua",
            "\"Chromium\";v=\"136\", \"Google Chrome\";v=\"136\", \"Not.A/Brand\";v=\"99\"");

        headers.TryAddWithoutValidation(
            "sec-ch-ua-mobile",
            "?0");

        headers.TryAddWithoutValidation(
            "sec-ch-ua-platform",
            "\"Windows\"");
    }

    
    public async Task<StreamInfo> DownloadVideoAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        var videoRequest = new HttpRequestMessage(
            HttpMethod.Get,
            url)
        {
            Version = HttpVersion.Version11
        };

        videoRequest.Headers.Referrer = new Uri("https://www.tiktok.com/");

        var videoResponse = await _httpClient.SendAsync(
            videoRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!videoResponse.IsSuccessStatusCode)
        {
            videoResponse.Dispose();
            throw new HttpRequestException(
                $"Failed to download video. Status code: {videoResponse.StatusCode}");
        }

        var contentLength = videoResponse.Content.Headers.ContentLength ?? -1;
        var stream = await videoResponse.Content.ReadAsStreamAsync(cancellationToken);
        return new StreamInfo
        {
            Stream          = stream,
            ContentLength   = contentLength
        }; 
    }

    /// <summary>
    /// Injects cookies with their correct domain into the session cookie container.
    /// </summary>
    public void InjectCookies(IReadOnlyList<CookieData> cookies)
    {
        foreach (var cookie in cookies)
        {
            var cleanDomain = cookie.Domain.TrimStart('.');
            var uri = new Uri($"https://{cleanDomain}/");
            _cookies.Add(uri, new Cookie(cookie.Name, cookie.Value, cookie.Path, cookie.Domain));
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _handler.Dispose();
    }
}
