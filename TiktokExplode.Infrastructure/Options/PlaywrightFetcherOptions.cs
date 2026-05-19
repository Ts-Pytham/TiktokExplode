namespace TiktokExplode.Infrastructure.Options;

public sealed class PlaywrightFetcherOptions
{
    public string? BrowserChannel { get; init; }
    public bool Headless { get; init; } = true;
    public float PageTimeoutMs { get; init; } = 30_000;
}
