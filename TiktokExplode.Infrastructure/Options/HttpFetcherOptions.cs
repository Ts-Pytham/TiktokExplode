namespace TiktokExplode.Infrastructure.Options;

/// <summary>
/// Configuration options for <c>HttpFetcher</c>.
/// </summary>
public sealed class HttpFetcherOptions
{
    /// <summary>
    /// How long to wait after the warmup request to <c>tiktok.com</c> before issuing
    /// the actual video page request. This delay lets TikTok's CDN register the session
    /// cookies before they are needed.
    /// Defaults to <c>1 200 ms</c>.
    /// </summary>
    public TimeSpan WarmupDelay { get; set; } = TimeSpan.FromMilliseconds(1200);

    /// <summary>
    /// The <c>User-Agent</c> header value sent with all HTTP requests.
    /// Defaults to a recent Chrome on Windows UA string.
    /// </summary>
    public string UserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36";
}
