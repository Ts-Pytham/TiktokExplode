# TiktokExplode.Extensions.DependencyInjection

[![NuGet](https://img.shields.io/nuget/v/TiktokExplode.Extensions.DependencyInjection.svg)](https://www.nuget.org/packages/TiktokExplode.Extensions.DependencyInjection)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0-512BD4)](https://dotnet.microsoft.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/Ts-Pytham/TiktokExplode/blob/master/LICENSE)

`Microsoft.Extensions.DependencyInjection` integration for [TiktokExplode](https://github.com/Ts-Pytham/TiktokExplode).

Provides `AddTiktokExplode()` — a fluent extension method on `IServiceCollection` that registers `IVideoClient` and optionally `ISearchClient`, and lets you choose independently between the Playwright or HTTP fetcher strategy for page fetching and search.

---

## Installation

```
dotnet add package TiktokExplode.Extensions.DependencyInjection
```

---

## Usage

```csharp
// Default — Playwright fetcher for page fetching, all defaults
services.AddTiktokExplode();

// Playwright fetcher with a visible browser window (useful for debugging)
services.AddTiktokExplode(b => b
    .UsePlaywrightFetcher(o => o.Headless = false));

// HTTP fetcher for page fetching (faster, no browser dependency)
// with Playwright search support — recommended setup
services.AddTiktokExplode(b => b
    .UseHttpFetcher()
    .UsePlaywrightSearch(o => o.BrowserChannel = "msedge"));

// HTTP fetcher only — no search support
// Note: ISearchClient is NOT registered without UsePlaywrightSearch
services.AddTiktokExplode(b => b
    .UseHttpFetcher(o => o.WarmupDelay = TimeSpan.Zero)
    .ConfigureTiktok(o => o.MaxWafRetries = 5));
```

Then inject `IVideoClient` or `ISearchClient` normally:

```csharp
public class MyService(IVideoClient client, ISearchClient search)
{
    public async Task<string> GetTitleAsync(string url)
    {
        var video = await client.GetVideoAsync(url);
        return video.Description;
    }

    public async IAsyncEnumerable<string> SearchTitlesAsync(string keyword)
    {
        await foreach (var video in search.SearchAsync(keyword))
            yield return video.Description;
    }
}
```

---

## Registered services

| Service                                            | Implementation                       | Lifetime  | Condition                    |
| -------------------------------------------------- | ------------------------------------ | --------- | ---------------------------- |
| `IVideoClient`                                     | `TiktokClient`                       | Singleton | Always                       |
| `ISearchClient`                                    | `TiktokSearchClient`                 | Singleton | `UsePlaywrightSearch` only   |
| `IPageFetcher`                                     | `PlaywrightFetcher` or `HttpFetcher` | Singleton | Always                       |
| `ISearchFetcher`                                   | `PlaywrightSearchFetcher`            | Singleton | `UsePlaywrightSearch` only   |
| `TiktokDownloadClient`                             | —                                    | Singleton | Always (shared HTTP session) |
| `TiktokOptions`                                    | —                                    | Singleton | Always                       |
| `PlaywrightFetcherOptions` or `HttpFetcherOptions` | —                                    | Singleton | Always                       |

> **Important:** `ISearchClient` and `ISearchFetcher` are only registered when `UsePlaywrightSearch` is called. TikTok's search API requires a real browser to sign requests — it cannot be replicated with plain HTTP. If you omit `UsePlaywrightSearch` and attempt to inject `ISearchClient`, the DI container will throw a resolution error at runtime.

> **Cookie sharing:** `TiktokDownloadClient` is registered as a singleton shared between `IVideoClient` and `ISearchClient`. Cookies obtained during a search session are automatically available when downloading, avoiding `403 Forbidden` errors.

---

## Builder API

| Method                                                    | Description                                                                                             |
| --------------------------------------------------------- | ------------------------------------------------------------------------------------------------------- |
| `ConfigureTiktok(Action<TiktokOptions>?)`                 | Configures WAF retry count and base delay                                                               |
| `UsePlaywrightFetcher(Action<PlaywrightFetcherOptions>?)` | Uses a real Chromium browser as `IPageFetcher` (default). Does **not** register search services         |
| `UseHttpFetcher(Action<HttpFetcherOptions>?)`             | Uses a plain `HttpClient` as `IPageFetcher` — faster start, no browser dependency, may be WAF-blocked   |
| `UsePlaywrightSearch(Action<PlaywrightFetcherOptions>?)`  | Registers `ISearchFetcher` and `ISearchClient` using Playwright. Independent of the page fetcher choice |

> **Note:** `UsePlaywrightFetcher` and `UseHttpFetcher` are mutually exclusive — the last one called wins. `UsePlaywrightSearch` is independent and can be combined with either.

---

## Related packages

| Package                                                                                       | Description                                              |
| --------------------------------------------------------------------------------------------- | -------------------------------------------------------- |
| [`TiktokExplode`](https://www.nuget.org/packages/TiktokExplode)                               | Domain layer — models, interfaces, exceptions            |
| [`TiktokExplode.Infrastructure`](https://www.nuget.org/packages/TiktokExplode.Infrastructure) | HTTP + Playwright fetchers, parser, download client      |
| [`TiktokExplode.All`](https://www.nuget.org/packages/TiktokExplode.All)                       | Meta-package — installs domain + infrastructure together |

---

## License

MIT — see [LICENSE](https://github.com/Ts-Pytham/TiktokExplode/blob/master/LICENSE) for details.
