namespace TiktokExplode.Infrastructure.Options;

/// <summary>
/// Configuration options that control the retry and request behaviour of <c>TiktokClient</c>.
/// </summary>
public sealed class TiktokOptions
{
    private int _maxRetries = 3;
    private TimeSpan _retryBaseDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The maximum number of times a request is retried after a
    /// <see cref="Domain.Exceptions.TiktokException.IsTransient">transient</see> failure
    /// (WAF challenge, soft block, rate limit, server error).
    /// Terminal failures such as a deleted video are never retried.
    /// The total number of attempts is <c>MaxRetries + 1</c>. Defaults to <c>3</c>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when set to a negative value.</exception>
    public int MaxRetries
    {
        get => _maxRetries;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            _maxRetries = value;
        }
    }

    /// <summary>
    /// The maximum number of times the client will retry a request after detecting a WAF challenge.
    /// </summary>
    [Obsolete($"Retries are no longer WAF-specific. Use {nameof(MaxRetries)} instead. This property will be removed in 2.0.")]
    public int MaxWafRetries
    {
        get => MaxRetries;
        set => MaxRetries = value;
    }

    /// <summary>
    /// The base delay between retry attempts. Each retry multiplies this value by its
    /// 1-based attempt number, so the delays grow linearly
    /// (e.g. 2 s, 4 s, 6 s for a base of 2 s).
    /// Defaults to <c>2 seconds</c>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when set to a negative value.</exception>
    public TimeSpan RetryBaseDelay
    {
        get => _retryBaseDelay;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, TimeSpan.Zero);
            _retryBaseDelay = value;
        }
    }

    /// <summary>
    /// When <see langword="true"/>, each retry delay is randomized within ±25% of its computed value
    /// so that concurrent clients do not retry in lockstep after a shared block.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool UseRetryJitter { get; set; } = true;
}