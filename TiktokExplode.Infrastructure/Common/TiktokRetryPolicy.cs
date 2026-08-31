using TiktokExplode.Domain.Exceptions;
using TiktokExplode.Infrastructure.Options;

namespace TiktokExplode.Infrastructure.Common;

/// <summary>
/// Shared retry loop for operations that talk to TikTok.
/// Only <see cref="TiktokException.IsTransient"/> failures are retried; everything else
/// — a deleted video, a changed page structure, a 404 — propagates on the first attempt.
/// </summary>
internal static class TiktokRetryPolicy
{
    /// <summary>
    /// Invokes <paramref name="operation"/>, retrying transient failures up to
    /// <see cref="TiktokOptions.MaxRetries"/> times with a linearly growing, optionally jittered delay.
    /// </summary>
    /// <typeparam name="T">The result type of the operation.</typeparam>
    /// <param name="operation">The operation to execute. Receives the cancellation token.</param>
    /// <param name="options">Retry count, base delay and jitter settings.</param>
    /// <param name="cancellationToken">Token to cancel the operation and the delays between attempts.</param>
    /// <returns>The result of the first successful attempt.</returns>
    public static async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        TiktokOptions options,
        CancellationToken cancellationToken)
    {
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                return await operation(cancellationToken);
            }
            catch (TiktokException ex) when (ex.IsTransient && attempt < options.MaxRetries)
            {
                await Task.Delay(GetDelay(options, attempt), cancellationToken);
            }
        }
    }

    private static TimeSpan GetDelay(TiktokOptions options, int attempt)
    {
        var delay = options.RetryBaseDelay * (attempt + 1);

        if (!options.UseRetryJitter || delay == TimeSpan.Zero)
            return delay;

        var factor = 0.75 + (Random.Shared.NextDouble() * 0.5);
        return delay * factor;
    }
}
