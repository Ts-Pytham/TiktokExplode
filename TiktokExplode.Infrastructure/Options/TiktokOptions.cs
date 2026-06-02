namespace TiktokExplode.Infrastructure.Options;

/// <summary>
/// Configuration options that control the retry and request behaviour of <c>TiktokClient</c>.
/// </summary>
public sealed class TiktokOptions
{
    /// <summary>
    /// The maximum number of times the client will retry a request after detecting a WAF challenge.
    /// The total number of attempts is <c>MaxWafRetries + 1</c>.
    /// Defaults to <c>3</c>.
    /// </summary>
    public int MaxWafRetries { get; set; } = 3;

    /// <summary>
    /// The base delay between WAF retry attempts. Each retry multiplies this value by its
    /// 1-based attempt number, so the delays grow linearly
    /// (e.g. 2 s, 4 s, 6 s for a base of 2 s).
    /// Defaults to <c>2 seconds</c>.
    /// </summary>
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromSeconds(2);
}