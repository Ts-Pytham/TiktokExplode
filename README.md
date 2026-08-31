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
- **Search TikTok by keyword** — streams results lazily via `IAsyncEnumerable<Media>` (videos and carousels)
- **Carousel/slideshow support** — `Carousel` entity with full image collection metadata
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
    new TikTokOptions { MaxRetries = 5 });

// HTTP-only — lightweight, may be blocked by WAF
await using var client = TiktokClient.CreateWithHttp();

// Inject your own IPageFetcher implementation
await using var client = new TiktokClient(myFetcher, new TikTokOptions());
```

#### Methods

| Method                                                 | Returns      | Description                       |
| ------------------------------------------------------ | ------------ | --------------------------------- |
| `GetVideoAsync(string url, CancellationToken)`         | `Video`      | Fetches full video metadata       |
| `DownloadAsync(Video, CancellationToken)`              | `StreamInfo` | Downloads video without watermark |
| `DownloadWatermarkedAsync(Video, CancellationToken)`   | `StreamInfo` | Downloads video with watermark    |
| `DownloadImageAsync(CarouselImage, CancellationToken)` | `StreamInfo` | Downloads a carousel image stream |

`TiktokClient` implements `IAsyncDisposable` — always use `await using`.

---

### `TiktokSearchClient`

`TiktokSearchClient` searches TikTok by keyword and streams results as `Media` objects (either `Video` or `Carousel`) using `IAsyncEnumerable<Media>`. It **requires Playwright** because TikTok's search API signs requests with dynamic tokens that can only be generated by a real browser session.

```csharp
await using var client = TiktokSearchClient.CreateWithBrowser();

await foreach (var media in client.SearchAsync("funny cats"))
{
    Console.WriteLine($"{media.Author.UniqueId}: {media.Description}");

    if (media is Video video)
    {
        // Standard video post
        await client.DownloadAsync(video, $"{video.Id}.mp4");
    }
    else if (media is Carousel carousel)
    {
        // Slideshow post — contains a collection of images
        Console.WriteLine($"Carousel with {carousel.Post.Images.Count} images");
    }
}
```

> **Note:** `TiktokSearchClient` implements `ISearchClient`, which extends `IDownloadClient`. This means all extension methods (`DownloadAsync(filePath)`, `DownloadWatermarkedAsync(filePath)`, `DownloadImageAsync`, `DownloadAnimatedCoverAsync`, `DownloadCarouselImagesAsync`) are available directly on the search client — no need for a separate `TiktokClient` instance.

> **Search scope:** The current implementation returns results from the **first page** of TikTok's search API (~10–20 results).

#### Methods

| Method                                                 | Returns                   | Description                                                  |
| ------------------------------------------------------ | ------------------------- | ------------------------------------------------------------ |
| `SearchAsync(string keyword, CancellationToken)`       | `IAsyncEnumerable<Media>` | Streams media results (videos and carousels) for the keyword |
| `DownloadAsync(Video, CancellationToken)`              | `StreamInfo`              | Downloads video without watermark                            |
| `DownloadWatermarkedAsync(Video, CancellationToken)`   | `StreamInfo`              | Downloads video with watermark                               |
| `DownloadImageAsync(CarouselImage, CancellationToken)` | `StreamInfo`              | Downloads a carousel image stream                            |

`TiktokSearchClient` implements `IAsyncDisposable` — always use `await using`.

#### Extension methods (via `DownloadClientExtensions`)

| Method                                                                | Description                                                         |
| --------------------------------------------------------------------- | ------------------------------------------------------------------- |
| `DownloadAsync(video, filePath, progress?, ct)`                       | Downloads video without watermark to a file, with optional progress |
| `DownloadWatermarkedAsync(video, filePath, progress?, ct)`            | Downloads video with watermark to a file, with optional progress    |
| `DownloadImageAsync(image, filePath, progress?, ct)`                  | Downloads a single carousel image to a file, with optional progress |
| `DownloadCarouselImagesAsync(carousel, directoryPath, progress?, ct)` | Downloads all carousel images to a directory with overall progress  |
| `DownloadCoverAsync(video, filePath, ct)`                             | Downloads the static cover image of a video (JPEG)                  |
| `DownloadAnimatedCoverAsync(video, filePath, ct)`                     | Downloads the animated cover of a video (WebP)                      |

> **Deprecated:** `DownloadImageAsync(video, …)` and `DownloadAnimatedImageAsync(video, …)` were renamed to
> `DownloadCoverAsync` and `DownloadAnimatedCoverAsync` — they download the _cover_, not a post image.
> The old names still work but will be removed in 2.0.

```csharp
// Download video to file with optional progress
IProgress<double> progress = new Progress<double>(p => Console.Write($"\rProgress: {p:P0}"));
await client.DownloadAsync(video, "output.mp4", progress);
await client.DownloadWatermarkedAsync(video, "output_wm.mp4", progress);

// Download cover images
await client.DownloadCoverAsync(video, "cover.jpg");
await client.DownloadAnimatedCoverAsync(video, "cover.webp");

// Download a single carousel image
await client.DownloadImageAsync(carousel.Post.Images[0], "image_1.jpg", progress);

// Download all carousel images to a directory (named image_1.jpg, image_2.jpg, ...)
await client.DownloadCarouselImagesAsync(carousel, "my_carousel_folder", progress);
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

| Property         | Default | Description                                                             |
| ---------------- | ------- | ----------------------------------------------------------------------- |
| `MaxRetries`     | `3`     | Max retries on **transient** failures (WAF, soft block, `429`, `5xx`)   |
| `RetryBaseDelay` | `2s`    | Base delay between retries (grows linearly)                             |
| `UseRetryJitter` | `true`  | Randomizes each delay by ±25% so concurrent clients don't retry in sync |

> **Deprecated:** `MaxWafRetries` still works but forwards to `MaxRetries` and will be removed in 2.0.
> Retries are no longer WAF-specific — see [Error Handling](#error-handling).

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

### `Media` model (base)

All search results are `Media` objects. Use pattern matching (`is Video`, `is Carousel`) to access type-specific properties.

| Property      | Type             | Description                                        |
| ------------- | ---------------- | -------------------------------------------------- |
| `Id`          | `string`         | TikTok post ID                                     |
| `Description` | `string`         | Caption / description                              |
| `Author`      | `Author`         | Author entity                                      |
| `Stats`       | `MediaStats`     | Views, likes, comments, shares, favorites, reposts |
| `Language`    | `MediaLanguage`  | Detected content language                          |
| `Location`    | `string`         | Location tag (if any)                              |
| `Cover`       | `MediaCover`     | Static (JPEG) and animated (WebP) cover image URLs |
| `Music`       | `MediaMusic`     | Track metadata and artwork                         |
| `CreatedAt`   | `DateTimeOffset` | Upload date                                        |

### `Video : Media` model

| Property   | Type            | Description                                 |
| ---------- | --------------- | ------------------------------------------- |
| `Info`     | `VideoInfo`     | Technical info, bitrates, and download URLs |
| `Duration` | `VideoDuration` | Duration in seconds and precise seconds     |

### `Carousel : Media` model

Represents a TikTok slideshow post (multiple images + audio).

| Property | Type           | Description                          |
| -------- | -------------- | ------------------------------------ |
| `Post`   | `CarouselPost` | Collection of images with their URLs |

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
// Note: ISearchClient is NOT available with HTTP fetcher (see below)
services.AddTiktokExplode(b => b
    .UseHttpFetcher(o => o.WarmupDelay = TimeSpan.Zero)
    .ConfigureTiktok(o => o.MaxRetries = 5));
```

Registered services:

| Service                                            | Implementation                       | Lifetime  | Fetcher             |
| -------------------------------------------------- | ------------------------------------ | --------- | ------------------- |
| `IVideoClient`                                     | `TiktokClient`                       | Singleton | Both                |
| `ISearchClient`                                    | `TiktokSearchClient`                 | Singleton | **Playwright only** |
| `IPageFetcher`                                     | `PlaywrightFetcher` or `HttpFetcher` | Singleton | Both                |
| `ISearchFetcher`                                   | `PlaywrightSearchFetcher`            | Singleton | **Playwright only** |
| `TikTokOptions`                                    | —                                    | Singleton | Both                |
| `PlaywrightFetcherOptions` or `HttpFetcherOptions` | —                                    | Singleton | Both                |

> **Important:** `ISearchClient` and `ISearchFetcher` are only registered when using the Playwright fetcher (the default). If you call `UseHttpFetcher()`, these services will **not** be present in the container. Attempting to inject `ISearchClient` with an HTTP-configured builder will throw a DI resolution error at runtime. This is by design — TikTok's search API requires a real browser to generate signed requests.

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

Every library error derives from `TiktokException`, which exposes `IsTransient` — `true` when the
same request is likely to succeed on a later attempt. The client retries **only** transient failures,
so a deleted video fails immediately instead of burning the full retry budget.

| Exception                        | Transient | Meaning                                                              |
| -------------------------------- | :-------: | -------------------------------------------------------------------- |
| `TiktokWafException`             |     ✔     | Bot detection challenge                                              |
| `TiktokUnavailablePageException` |     ✔     | Soft block: TikTok served a placeholder page or an empty payload     |
| `TiktokHttpException`            |  depends  | Non-success HTTP status. Transient for `408`, `425`, `429` and `5xx` |
| `VideoNotFoundException`         |     ✘     | Video removed, private, or region-locked                             |
| `TiktokParsingException`         |     ✘     | TikTok changed its HTML/JSON — the library needs updating            |

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
catch (TiktokUnavailablePageException ex)
{
    // TikTok soft-blocked the request after all retries exhausted
}
catch (TiktokHttpException ex)
{
    // ex.StatusCode carries the HTTP status returned by TikTok or the CDN
}
catch (MediaNotFoundException ex)
{
    // Video or carousel does not exist or is private.
    // ex.TiktokStatusCode carries TikTok's own reason code when available.
}
catch (TiktokParsingException ex)
{
    // Unexpected page structure. ex.Path points at the node that could not be resolved.
}
catch (TiktokException ex)
{
    // Base exception — catch-all for library errors. ex.IsTransient tells you whether to retry.
}
```

---

## Project Structure

```
TiktokExplode/                # Domain — zero external dependencies
  Domain/
    Entities/                 # Media (abstract), Video, Carousel, Author
    ValueObjects/
      Media/                  # MediaStats, MediaLanguage, MediaCover, MediaMusic, MediaDuration
      Videos/                 # VideoInfo, VideoDuration, VideoDownloadLinks, Bitrate
      Carousels/              # CarouselPost and related types
      Authors/                # AuthorStats, ProfileImageVariants
    Abstractions/             # IVideoClient, ISearchClient, IDownloadClient
    Exceptions/               # TiktokException hierarchy
    Utilities/                # URL validation

TiktokExplode.Infrastructure/ # HTTP + browser automation (Playwright + AngleSharp)
  Clients/                    # TiktokClient : IVideoClient, TiktokSearchClient : ISearchClient
  Fetchers/                   # IPageFetcher, PlaywrightFetcher, HttpFetcher
  Fetchers/Search/            # ISearchFetcher, PlaywrightSearchFetcher
  Http/                       # TikTokDownloadClient — CDN download management
  Browser/                    # TikTokBrowser — internal Playwright wrapper
  Parsers/                    # TikTokVideoParser, TikTokSearchParser — JSON extraction
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
