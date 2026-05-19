namespace TiktokExplode.Infrastructure.Options;

/// <summary>
/// Configuration options for <c>PlaywrightFetcher</c>.
/// </summary>
public sealed class PlaywrightFetcherOptions
{
    /// <summary>
    /// The browser channel to launch. Use <c>"msedge"</c> or <c>"chrome"</c> to use a locally
    /// installed browser, or <see langword="null"/> to use Playwright's bundled Chromium.
    /// Defaults to <see langword="null"/>.
    /// </summary>
    public string? BrowserChannel { get; init; }

    /// <summary>
    /// Whether to run the browser in headless mode (no visible window).
    /// Set to <see langword="false"/> to show the browser during debugging.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool Headless { get; init; } = true;

    /// <summary>
    /// Maximum time in milliseconds to wait for a page navigation to complete.
    /// Defaults to <c>30 000</c> ms (30 seconds).
    /// </summary>
    public float PageTimeoutMs { get; init; } = 30_000;
}
