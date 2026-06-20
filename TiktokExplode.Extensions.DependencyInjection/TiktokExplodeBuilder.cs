using Microsoft.Extensions.DependencyInjection;
using TiktokExplode.Domain.Abstractions;
using TiktokExplode.Infrastructure.Clients;
using TiktokExplode.Infrastructure.Fetchers;
using TiktokExplode.Infrastructure.Fetchers.Search;
using TiktokExplode.Infrastructure.Http;
using TiktokExplode.Infrastructure.Options;

namespace TiktokExplode.Extensions.DependencyInjection;

/// <summary>
/// Fluent builder for configuring TiktokExplode services before they are registered
/// into an <see cref="IServiceCollection"/>.
/// Obtain an instance through <see cref="TiktokExplodeServiceCollectionExtensions.AddTiktokExplode"/>.
/// </summary>
public sealed class TiktokExplodeBuilder(IServiceCollection services)
{
    private readonly TiktokOptions _tiktokOptions = new();
    private Action<IServiceCollection> _pageFetcherRegistration = RegisterPlaywright(new());
    private Action<IServiceCollection>? _searchRegistration;

    /// <summary>
    /// Configures the WAF-retry behaviour of <c>TiktokClient</c>.
    /// </summary>
    /// <param name="options">Delegate that mutates a <see cref="TiktokOptions"/> instance.</param>
    /// <returns>The same builder for chaining.</returns>
    public TiktokExplodeBuilder ConfigureTiktok(Action<TiktokOptions>? options = null)
    {
        options?.Invoke(_tiktokOptions);
        return this;
    }

    /// <summary>
    /// Configures the Playwright-based page fetcher as the active <see cref="IPageFetcher"/>.
    /// Use this only when you need a real browser to fetch video pages.
    /// For search support, chain <see cref="UsePlaywrightSearch"/> independently.
    /// </summary>
    /// <param name="options">Delegate that mutates a <see cref="PlaywrightFetcherOptions"/> instance.</param>
    /// <returns>The same builder for chaining.</returns>
    public TiktokExplodeBuilder UsePlaywrightFetcher(Action<PlaywrightFetcherOptions>? options = null)
    {
        var playwrightOptions = new PlaywrightFetcherOptions();
        options?.Invoke(playwrightOptions);
        _pageFetcherRegistration = RegisterPlaywright(playwrightOptions);
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
        _pageFetcherRegistration = RegisterHttp(httpFetcherOptions);
        return this;
    }

    /// <summary>
    /// Enables TikTok search support using a Playwright-based browser session.
    /// Registers <see cref="ISearchFetcher"/> and <see cref="ISearchClient"/> as singletons.
    /// Can be combined with any <see cref="IPageFetcher"/> strategy.
    /// </summary>
    /// <param name="options">Delegate that mutates a <see cref="PlaywrightFetcherOptions"/> instance.</param>
    /// <returns>The same builder for chaining.</returns>
    public TiktokExplodeBuilder UsePlaywrightSearch(Action<PlaywrightFetcherOptions>? options = null)
    {
        var playwrightOptions = new PlaywrightFetcherOptions();
        options?.Invoke(playwrightOptions);
        _searchRegistration = RegisterPlaywrightSearch(playwrightOptions);
        return this;
    }

    /// <summary>
    /// Applies all pending registrations to the underlying <see cref="IServiceCollection"/>.
    /// Registers <see cref="TiktokOptions"/>, the chosen <see cref="IPageFetcher"/>,
    /// <see cref="TiktokDownloadClient"/> (shared session), and <see cref="IVideoClient"/> as singletons.
    /// </summary>
    /// <returns>The service collection for further chaining.</returns>
    public IServiceCollection Build()
    {
        services.AddSingleton(_tiktokOptions);
        services.AddSingleton<TiktokDownloadClient>();
        _pageFetcherRegistration(services);
        _searchRegistration?.Invoke(services);

        services.AddSingleton<IVideoClient>(sp => new TiktokClient(
            sp.GetRequiredService<TiktokDownloadClient>(),
            sp.GetRequiredService<IPageFetcher>(),
            sp.GetRequiredService<TiktokOptions>()));
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

    private static Action<IServiceCollection> RegisterPlaywrightSearch(PlaywrightFetcherOptions options)
    {
        return services =>
        {
            services.AddSingleton(options);
            services.AddSingleton<ISearchFetcher, PlaywrightSearchFetcher>();
            services.AddSingleton<ISearchClient>(sp => new TiktokSearchClient(
                sp.GetRequiredService<TiktokDownloadClient>(),
                sp.GetRequiredService<ISearchFetcher>()));
        };
    }
}
