using Microsoft.Extensions.DependencyInjection;

namespace TiktokExplode.Extensions.DependencyInjection;

/// <summary>
/// Extension methods on <see cref="IServiceCollection"/> for registering TiktokExplode services.
/// </summary>
public static class TiktokExplodeServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers TiktokExplode services into the service collection.
        /// By default, uses the Playwright-based fetcher with default options.
        /// </summary>
        /// <param name="configure">
        /// Optional delegate to customise the fetcher strategy and options via <see cref="TiktokExplodeBuilder"/>.
        /// </param>
        /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
        /// <example>
        /// <code>
        /// // Default — Playwright fetcher, all defaults
        /// services.AddTiktokExplode();
        ///
        /// // Custom — HTTP fetcher, no warmup delay
        /// services.AddTiktokExplode(b => b
        ///     .UseHttpFetcher(o => o.WarmupDelay = TimeSpan.Zero)
        ///     .ConfigureTiktok(o => o.MaxWafRetries = 5));
        /// </code>
        /// </example>
        public IServiceCollection AddTiktokExplode(
            Action<TiktokExplodeBuilder>? configure = null)
        {
            var builder = new TiktokExplodeBuilder(services);
            configure?.Invoke(builder);

            return builder.Build();
        }
    }
}
