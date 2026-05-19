# TiktokExplode

[![NuGet](https://img.shields.io/nuget/v/TiktokExplode.svg?label=TiktokExplode)](https://www.nuget.org/packages/TiktokExplode)
[![NuGet](https://img.shields.io/nuget/v/TiktokExplode.Infrastructure.svg?label=TiktokExplode.Infrastructure)](https://www.nuget.org/packages/TiktokExplode.Infrastructure)
[![NuGet](https://img.shields.io/nuget/v/TiktokExplode.All.svg?label=TiktokExplode.All)](https://www.nuget.org/packages/TiktokExplode.All)
[![NuGet](https://img.shields.io/nuget/v/TiktokExplode.Extensions.DependencyInjection.svg?label=TiktokExplode.Extensions.DependencyInjection)](https://www.nuget.org/packages/TiktokExplode.Extensions.DependencyInjection)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0-512BD4)](https://dotnet.microsoft.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

<p align="center">
  <img src="assets/icon.png" alt="TiktokExplode" width="140" />
</p>

**TiktokExplode** is a .NET library that lets you retrieve metadata and download videos from TikTok programmatically. It handles session management, cookie injection, and WAF/bot-detection bypassing behind a simple, clean API — so you can focus on using the data instead of fighting the platform.

The library follows **Clean Architecture**: the domain layer (`TiktokExplode`) has zero external dependencies and exposes immutable, strongly-typed models, while the infrastructure layer (`TiktokExplode.Infrastructure`) handles all HTTP and browser-based concerns.

---

## Features

- Fetch full video metadata: author, stats, duration, language, location, bitrates, and more
- Download videos without watermark or with watermark
- Download the static cover image (JPEG) or the animated cover (WebP)
- Progress reporting during download via `IProgress<double>`
- Automatic WAF/bot-detection retry with configurable backoff
- **Strategy pattern** — choose between Playwright (reliable) or HTTP-only (lightweight) page fetching
- Clean Architecture — pure domain with zero external dependencies
- Targets **net8.0** and **net9.0**

---

## Installation

**Recommended — install both packages in one command:**

```
dotnet add package TiktokExplode.All
```

**Or install individually:**

```
dotnet add package TiktokExplode.Infrastructure
```

> `TiktokExplode.Infrastructure` automatically brings in `TiktokExplode` (domain) as a transitive dependency.  
> Install `TiktokExplode` alone only if you need the domain models/interfaces without the infrastructure.

**Using Microsoft.Extensions.DependencyInjection?**

```
dotnet add package TiktokExplode.Extensions.DependencyInjection
```

> Adds `AddTiktokExplode()` on `IServiceCollection`. See the [Dependency Injection](#dependency-injection) section.

> **Note:** `TiktokExplode.Infrastructure` depends on [Microsoft.Playwright](https://playwright.dev/dotnet/). After installation, run the following once to download the browser binaries:
>
> ```
> pwsh -c "playwright install chromium"
> ```

---

## Quick Start

```csharp
using TiktokExplode.Infrastructure.Clients;
using TiktokExplode.Infrastructure.Common;

await using var client = TiktokClient.CreateWithBrowser();

var video = await client.GetVideoAsync("https://www.tiktok.com/@user/video/1234567890");

Console.WriteLine($"ID:       {video.Id}");
Console.WriteLine($"Author:   {video.Author.Name} (@{video.Author.UniqueId})");
Console.WriteLine($"Duration: {video.Duration.Seconds}s");
Console.WriteLine($"Views:    {video.Stats.Views}");
Console.WriteLine($"Likes:    {video.Stats.Likes}");

// Download without watermark (stream + content length)
await using var streamInfo = await client.DownloadAsync(video);
await using var file = File.Create($"{video.Id}.mp4");
await streamInfo.Stream.CopyToAsync(file);

// Or use the extension to download directly to a file with progress
IProgress<double> progress = new Progress<double>(p => Console.Write($"\rProgress: {p:P0}"));
await client.DownloadAsync(video, $"{video.Id}.mp4", progress);
```

---

## API Reference

### `TiktokClient`

`TiktokClient` uses the **Strategy pattern** to decouple page fetching from downloading. Use the factory methods to choose a strategy:

```csharp
// Playwright — uses a real browser to bypass WAF (recommended)
await using var client = TiktokClient.CreateWithBrowser();

// Playwright with custom options
await using var client = TiktokClient.CreateWithBrowser(
    new PlaywrightFetcherOptions { BrowserChannel = "msedge", Headless = true },
    new TikTokOptions { MaxWafRetries = 5 });

// HTTP-only — lightweight, may be blocked by WAF
await using var client = TiktokClient.CreateWithHttp();

// Inject your own IPageFetcher implementation
await using var client = new TiktokClient(myFetcher, new TikTokOptions());
```

#### Methods

| Method                                               | Returns      | Description                       |
| ---------------------------------------------------- | ------------ | --------------------------------- |
| `GetVideoAsync(string url, CancellationToken)`       | `Video`      | Fetches full video metadata       |
| `DownloadAsync(Video, CancellationToken)`            | `StreamInfo` | Downloads video without watermark |
| `DownloadWatermarkedAsync(Video, CancellationToken)` | `StreamInfo` | Downloads video with watermark    |

`TiktokClient` implements `IAsyncDisposable` — always use `await using`.

#### Extension methods (via `TiktokClientExtensions`)

| Method | Description |
| --- | --- |
| `DownloadAsync(video, filePath, progress?, ct)` | Downloads video without watermark to a file, with optional progress |
| `DownloadWatermarkedAsync(video, filePath, progress?, ct)` | Downloads video with watermark to a file, with optional progress |
| `DownloadImageAsync(video, filePath, ct)` | Downloads the static cover image (JPEG) |
| `DownloadAnimatedImageAsync(video, filePath, ct)` | Downloads the animated cover (WebP) |

```csharp
// Download to file path with optional progress
await client.DownloadAsync(video, "output.mp4", progress, cancellationToken);
await client.DownloadWatermarkedAsync(video, "output_wm.mp4", progress, cancellationToken);

// Download cover images
await client.DownloadImageAsync(video, "cover.jpg");
await client.DownloadAnimatedImageAsync(video, "cover.webp");
```

> **Note:** Animated covers are served by TikTok as animated WebP files. Not all videos have an animated cover — if `Cover.AnimatedUrl` is empty, the video only has a static cover.

`ContentLength` is sourced from the CDN response headers — always accurate, no estimate from metadata.

---

### `StreamInfo`

Returned by `DownloadAsync` and `DownloadWatermarkedAsync`. Implements `IAsyncDisposable`.

| Property        | Type     | Description                       |
| --------------- | -------- | --------------------------------- |
| `Stream`        | `Stream` | The video content stream          |
| `ContentLength` | `long`   | Exact file size in bytes from CDN |

---

### `TikTokOptions`

| Property         | Default | Description                                 |
| ---------------- | ------- | ------------------------------------------- |
| `MaxWafRetries`  | `3`     | Max retries on WAF detection                |
| `RetryBaseDelay` | `2s`    | Base delay between retries (grows linearly) |

### `PlaywrightFetcherOptions`

| Property         | Default | Description                                                                              |
| ---------------- | ------- | ---------------------------------------------------------------------------------------- |
| `BrowserChannel` | `null`  | Browser channel (e.g. `"msedge"`, `"chrome"`). `null` uses Playwright's bundled Chromium |
| `Headless`       | `true`  | Run browser in headless mode                                                             |
| `PageTimeoutMs`  | `30000` | Navigation timeout in milliseconds                                                       |

### `HttpFetcherOptions`

| Property      | Default       | Description                                |
| ------------- | ------------- | ------------------------------------------ |
| `UserAgent`   | Chrome 136 UA | User-Agent header sent with requests       |
| `WarmupDelay` | `1200ms`      | Delay after warmup request before fetching |

---

### `Video` model

| Property      | Type             | Description                                        |
| ------------- | ---------------- | -------------------------------------------------- |
| `Id`          | `string`         | TikTok video ID                                    |
| `Description` | `string`         | Caption / description                              |
| `Author`      | `Author`         | Author entity                                      |
| `Duration`    | `VideoDuration`  | Duration in seconds and precise seconds            |
| `Stats`       | `VideoStats`     | Views, likes, comments, shares, favorites, reposts |
| `Info`        | `VideoInfo`      | Technical info, bitrates, and download URLs        |
| `Language`    | `VideoLanguage`  | Detected content language                          |
| `Location`    | `string`         | Location tag (if any)                              |
| `Cover`       | `VideoCover`     | Static (JPEG) and animated (WebP) cover image URLs |
| `CreatedAt`   | `DateTimeOffset` | Upload date                                        |

### `Author` model

| Property      | Type                   | Description                                                |
| ------------- | ---------------------- | ---------------------------------------------------------- |
| `Id`          | `string`               | Internal TikTok user ID                                    |
| `UniqueId`    | `string`               | Handle (e.g. `johndoe`)                                    |
| `Name`        | `string`               | Display name                                               |
| `Description` | `string`               | Bio                                                        |
| `IsVerified`  | `bool`                 | Verified badge                                             |
| `IsPrivate`   | `bool`                 | Private account                                            |
| `Avatar`      | `ProfileImageVariants` | Avatar image URLs (small, medium, large)                   |
| `Stats`       | `AuthorStats`          | Followers, following, friends, likes received, video count |
| `CreatedAt`   | `DateTimeOffset`       | Account creation date                                      |

---

## Dependency Injection

`TiktokExplode.Extensions.DependencyInjection` provides a fluent `AddTiktokExplode()` extension method for registering all TiktokExplode services into the .NET DI container.

```csharp
// Default — Playwright fetcher, all defaults
services.AddTiktokExplode();

// Custom — Playwright with visible browser window
services.AddTiktokExplode(b => b
    .UsePlaywrightFetcher(o => o.Headless = false));

// HTTP fetcher — lighter, no browser dependency
services.AddTiktokExplode(b => b
    .UseHttpFetcher(o => o.WarmupDelay = TimeSpan.Zero)
    .ConfigureTiktok(o => o.MaxWafRetries = 5));
```

Registered services:

| Service | Implementation | Lifetime |
| --- | --- | --- |
| `IVideoClient` | `TiktokClient` | Singleton |
| `IPageFetcher` | `PlaywrightFetcher` or `HttpFetcher` | Singleton |
| `TikTokOptions` | — | Singleton |
| `PlaywrightFetcherOptions` or `HttpFetcherOptions` | — | Singleton |

```csharp
// Consume in your services via constructor injection
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

## Error Handling

```csharp
using TiktokExplode.Domain.Exceptions;

try
{
    var video = await client.GetVideoAsync(url);
}
catch (TiktokWafException ex)
{
    // Bot detection triggered after all retries exhausted
}
catch (VideoNotFoundException ex)
{
    // Video does not exist or is private
}
catch (TiktokParsingException ex)
{
    // Unexpected page structure (TikTok changed their HTML/JSON)
}
catch (TiktokException ex)
{
    // Base exception — catch-all for library errors
}
```

---

## Project Structure

```
TiktokExplode/                # Domain — zero external dependencies
  Domain/
    Entities/                 # Video, Author
    ValueObjects/             # VideoInfo, StreamInfo, VideoStats, VideoDuration, etc.
    Abstractions/             # IVideoClient
    Exceptions/               # TiktokException hierarchy
    Utilities/                # URL validation

TiktokExplode.Infrastructure/ # HTTP + browser automation (Playwright + AngleSharp)
  Clients/                    # TiktokClient : IVideoClient
  Fetchers/                   # IPageFetcher, PlaywrightFetcher, HttpFetcher
  Http/                       # TikTokDownloadClient — CDN download management
  Browser/                    # TikTokBrowser — internal Playwright wrapper
  Parsers/                    # TikTokVideoParser — JSON extraction from hydration script
  Options/                    # TikTokOptions, PlaywrightFetcherOptions, HttpFetcherOptions
  Common/                     # StreamExtensions, TiktokClientExtensions

TiktokExplode.All/            # Meta-package — installs both packages above in one command

TiktokExplode.Extensions.DependencyInjection/  # AddTiktokExplode() for Microsoft.Extensions.DI
```

---

## Acknowledgements

This library was heavily inspired by [**YoutubeExplode**](https://github.com/Tyrrrz/YoutubeExplode) by [Tyrrrz](https://github.com/Tyrrrz). His work showed me how a well-designed, clean .NET library for a media platform should look and feel. Without that reference, TiktokExplode would not exist. Thank you.

---

## License

MIT — see [LICENSE](LICENSE) for details.
