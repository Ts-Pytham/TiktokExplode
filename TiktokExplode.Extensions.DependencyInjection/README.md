# TiktokExplode.Extensions.DependencyInjection

[![NuGet](https://img.shields.io/nuget/v/TiktokExplode.Extensions.DependencyInjection.svg)](https://www.nuget.org/packages/TiktokExplode.Extensions.DependencyInjection)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0-512BD4)](https://dotnet.microsoft.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/Ts-Pytham/TiktokExplode/blob/master/LICENSE)

`Microsoft.Extensions.DependencyInjection` integration for [TiktokExplode](https://github.com/Ts-Pytham/TiktokExplode).

Provides `AddTiktokExplode()` — a fluent extension method on `IServiceCollection` that registers `IVideoClient` and lets you choose between the Playwright (default) or HTTP fetcher strategy.

---

## Installation

```
dotnet add package TiktokExplode.Extensions.DependencyInjection
```

---

## Usage

```csharp
// Default — Playwright fetcher, all defaults
services.AddTiktokExplode();

// Playwright with a visible browser window (useful for debugging)
services.AddTiktokExplode(b => b
    .UsePlaywrightFetcher(o => o.Headless = false));

// HTTP fetcher — no browser dependency, lighter footprint
services.AddTiktokExplode(b => b
    .UseHttpFetcher(o => o.WarmupDelay = TimeSpan.Zero)
    .ConfigureTiktok(o => o.MaxWafRetries = 5));
```

Then inject `IVideoClient` normally:

```csharp
public class MyService(IVideoClient client)
{
    public async Task<string> GetTitleAsync(string url)
    {
        var video = await client.GetVideoAsync(url);
        return video.Description;
    }
}
```

---

## Registered services

| Service | Implementation | Lifetime |
| --- | --- | --- |
| `IVideoClient` | `TiktokClient` | Singleton |
| `IPageFetcher` | `PlaywrightFetcher` or `HttpFetcher` | Singleton |
| `TikTokOptions` | — | Singleton |
| `PlaywrightFetcherOptions` or `HttpFetcherOptions` | — | Singleton |

---

## Builder API

| Method | Description |
| --- | --- |
| `ConfigureTiktok(Action<TikTokOptions>?)` | Configures WAF retry count and base delay |
| `UsePlaywrightFetcher(Action<PlaywrightFetcherOptions>?)` | Uses a real Chromium browser via Playwright (default) |
| `UseHttpFetcher(Action<HttpFetcherOptions>?)` | Uses a plain `HttpClient` — faster start, may be WAF-blocked |

> **Note:** `UsePlaywrightFetcher` and `UseHttpFetcher` are mutually exclusive — the last one called wins.

---

## Related packages

| Package | Description |
| --- | --- |
| [`TiktokExplode`](https://www.nuget.org/packages/TiktokExplode) | Domain layer — models, interfaces, exceptions |
| [`TiktokExplode.Infrastructure`](https://www.nuget.org/packages/TiktokExplode.Infrastructure) | HTTP + Playwright fetchers, parser, download client |
| [`TiktokExplode.All`](https://www.nuget.org/packages/TiktokExplode.All) | Meta-package — installs domain + infrastructure together |

---

## License

MIT — see [LICENSE](https://github.com/Ts-Pytham/TiktokExplode/blob/master/LICENSE) for details.
