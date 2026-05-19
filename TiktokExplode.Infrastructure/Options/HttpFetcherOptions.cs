namespace TiktokExplode.Infrastructure.Options;

public sealed class HttpFetcherOptions
{
    public TimeSpan WarmupDelay { get; init; } = TimeSpan.FromMilliseconds(1200);
    public string UserAgent { get; init; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36";
}
