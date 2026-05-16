# TiktokExplode

[![NuGet](https://img.shields.io/badge/nuget-TiktokExplode-blue)](https://www.nuget.org/packages/TiktokExplode)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0-512BD4)](https://dotnet.microsoft.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

**TiktokExplode** is a .NET library that lets you retrieve metadata and download videos from TikTok programmatically. It handles session management, cookie injection, and WAF/bot-detection bypassing behind a simple, clean API — so you can focus on using the data instead of fighting the platform.

The library follows **Clean Architecture**: the domain layer (`TiktokExplode`) has zero external dependencies and exposes immutable, strongly-typed models, while the infrastructure layer (`TiktokExplode.Infrastructure`) handles all HTTP and browser-based concerns.

---

## Features

- Fetch full video metadata: title, author, stats, duration, language, location, and more
- Download videos without watermark or with watermark
- Automatic WAF/bot-detection retry with configurable backoff
- Clean Architecture — pure domain with zero external dependencies
- Targets **net8.0** and **net9.0**

---

## Installation

```
dotnet add package TiktokExplode
dotnet add package TiktokExplode.Infrastructure
```

> `TiktokExplode` contains the domain models and interfaces.  
> `TiktokExplode.Infrastructure` contains the HTTP client and parsing logic.

---

## Quick Start

```csharp
using TiktokExplode.Infrastructure.Clients;

await using var client = new TiktokClient();

var video = await client.GetVideoAsync("https://www.tiktok.com/@user/video/1234567890");

Console.WriteLine($"ID:       {video.Id}");
Console.WriteLine($"Author:   {video.Author.Name} (@{video.Author.UniqueId})");
Console.WriteLine($"Duration: {video.Duration.Seconds}s");
Console.WriteLine($"Views:    {video.Stats.Views}");
Console.WriteLine($"Likes:    {video.Stats.Likes}");

// Download without watermark
await using var stream = await client.DownloadAsync(video);
await using var file = File.Create($"{video.Id}.mp4");
await stream.CopyToAsync(file);
```

---

## API Reference

### `TiktokClient`

```csharp
// Default options
var client = new TiktokClient();

// Custom options
var client = new TiktokClient(new TikTokOptions
{
    MaxWafRetries  = 5,
    RetryBaseDelay = TimeSpan.FromSeconds(3),
});
```

#### Methods

| Method | Description |
|--------|-------------|
| `GetVideoAsync(string url, CancellationToken)` | Fetches full video metadata |
| `DownloadAsync(Video video, CancellationToken)` | Downloads the video without watermark |
| `DownloadWatermarkedAsync(Video video, CancellationToken)` | Downloads the video with watermark |

`TiktokClient` implements `IAsyncDisposable` — use `await using` or dispose explicitly.

---

### `Video` model

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | TikTok video ID |
| `Description` | `string` | Caption / description |
| `Author` | `Author` | Author entity |
| `Duration` | `VideoDuration` | Duration in seconds |
| `Stats` | `VideoStats` | Views, likes, comments, shares |
| `Info` | `VideoInfo` | Technical info + download URLs |
| `Language` | `VideoLanguage` | Detected content language |
| `Location` | `string` | Location tag (if any) |
| `CreatedAt` | `DateTimeOffset` | Upload date |

### `Author` model

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Internal TikTok user ID |
| `UniqueId` | `string` | Handle (e.g. `johndoe`) |
| `Name` | `string` | Display name |
| `Description` | `string` | Bio |
| `IsVerified` | `bool` | Verified badge |
| `IsPrivate` | `bool` | Private account |
| `Avatar` | `ProfileImageVariants` | Avatar image URLs |
| `Stats` | `AuthorStats` | Followers, following, likes |
| `CreatedAt` | `DateTimeOffset` | Account creation date |

---

### `TikTokOptions`

| Property | Default | Description |
|----------|---------|-------------|
| `MaxWafRetries` | `3` | Max retries on WAF detection |
| `RetryBaseDelay` | `2s` | Base delay between retries (grows linearly) |
| `RequestDelay` | `1200ms` | Delay between warmup and actual request |

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
    // Bot detection triggered after all retries
}
catch (VideoNotFoundException ex)
{
    // Video does not exist or is private
}
catch (TiktokParsingException ex)
{
    // Unexpected page structure (TikTok changed their HTML)
}
catch (TiktokException ex)
{
    // Base exception — catch-all for library errors
}
```

---

## Project Structure

```
TiktokExplode/               # Domain — zero external dependencies
  Domain/
    Entities/                # Video, Author
    ValueObjects/            # VideoInfo, VideoStats, VideoDuration, etc.
    Abstractions/            # IVideoClient
    Exceptions/              # TiktokException hierarchy
    Utilities/               # URL validation

TiktokExplode.Infrastructure/ # HTTP + parsing (uses PuppeteerSharp / AngleSharp)
  Clients/                   # TiktokClient : IVideoClient
  Http/                      # TikTokSession — cookie & download management
  Browser/                   # TikTokBrowser — headless browser session
  Parsers/                   # TikTokVideoParser — JSON extraction
  Options/                   # TikTokOptions
```

---

## Acknowledgements

This library was heavily inspired by [**YoutubeExplode**](https://github.com/Tyrrrz/YoutubeExplode) by [Tyrrrz](https://github.com/Tyrrrz). His work showed me how a well-designed, clean .NET library for a media platform should look and feel. Without that reference, TiktokExplode would not exist. Thank you.

---

## License

MIT — see [LICENSE](LICENSE) for details.