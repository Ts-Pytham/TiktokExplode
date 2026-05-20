using Microsoft.Extensions.DependencyInjection;
using TiktokExplode.Domain.Abstractions;
using TiktokExplode.Infrastructure.Clients;
using TiktokExplode.Infrastructure.Fetchers;
using TiktokExplode.Infrastructure.Options;

namespace TiktokExplode.Extensions.DependencyInjection;

/// <summary>
/// Fluent builder for configuring TiktokExplode services before they are registered
/// into an <see cref="IServiceCollection"/>.
/// Obtain an instance through <see cref="TiktokExplodeServiceCollectionExtensions.AddTiktokExplode"/>.
/// </summary>
public sealed class TiktokExplodeBuilder(IServiceCollection services)
{
    private readonly TikTokOptions _tiktokOptions = new();
    private Action<IServiceCollection> _fetcherRegistration = RegisterPlaywright(new());

    /// <summary>
    /// Configures the WAF-retry behaviour of <c>TiktokClient</c>.
    /// </summary>
    /// <param name="options">Delegate that mutates a <see cref="TikTokOptions"/> instance.</param>
    /// <returns>The same builder for chaining.</returns>
    public TiktokExplodeBuilder ConfigureTiktok(Action<TikTokOptions>? options = null)
    {
        options?.Invoke(_tiktokOptions);
        return this;
    }

    /// <summary>
    /// Configures the Playwright-based page fetcher as the active <see cref="IPageFetcher"/>.
    /// This is the default strategy — call this only when you need to customise the options.
    /// </summary>
    /// <param name="options">Delegate that mutates a <see cref="PlaywrightFetcherOptions"/> instance.</param>
    /// <returns>The same builder for chaining.</returns>
    public TiktokExplodeBuilder UsePlaywrightFetcher(Action<PlaywrightFetcherOptions>? options = null)
    {
        var playwrightOptions = new PlaywrightFetcherOptions();
        options?.Invoke(playwrightOptions);
        _fetcherRegistration = RegisterPlaywright(playwrightOptions);
        return this;
    }

    /// <summary>
    /// Configures the lightweight HTTP-based page fetcher as the active <see cref="IPageFetcher"/>.
    /// This strategy avoids the Playwright browser dependency but may be blocked by TikTok's WAF.
    /// </summary>
    /// <param name="options">Delegate that mutates an <see cref="HttpFetcherOptions"/> instance.</param>
    /// <returns>The same builder for chaining.</returns>
    public TiktokExplodeBuilder UseHttpFetcher(Action<HttpFetcherOptions>? options = null)
    {
        var httpFetcherOptions = new HttpFetcherOptions();
        options?.Invoke(httpFetcherOptions);
        _fetcherRegistration = RegisterHttp(httpFetcherOptions);
        return this;
    }

    /// <summary>
    /// Applies all pending registrations to the underlying <see cref="IServiceCollection"/>.
    /// Registers <see cref="TikTokOptions"/>, the chosen <see cref="IPageFetcher"/>,
    /// and <see cref="IVideoClient"/> as singletons.
    /// </summary>
    /// <returns>The service collection for further chaining.</returns>
    public IServiceCollection Build()
    {
        services.AddSingleton(_tiktokOptions);
        _fetcherRegistration(services);
        services.AddSingleton<IVideoClient, TiktokClient>();
        return services;
    }

    private static Action<IServiceCollection> RegisterPlaywright(PlaywrightFetcherOptions options)
    {
        return services =>
        {
            services.AddSingleton(options);
            services.AddSingleton<IPageFetcher, PlaywrightFetcher>();
        };
    }

    private static Action<IServiceCollection> RegisterHttp(HttpFetcherOptions options)
    {
        return services =>
        {
            services.AddSingleton(options);
            services.AddSingleton<IPageFetcher, HttpFetcher>();
        };
    }
}
