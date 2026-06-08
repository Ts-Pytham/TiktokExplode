using TiktokExplode.Domain.Abstractions;
using TiktokExplode.Domain.Entities;
using TiktokExplode.Domain.Exceptions;
using TiktokExplode.Infrastructure.Clients;
using TiktokExplode.Infrastructure.Common;
using TiktokExplode.Infrastructure.Options;

const string Sep = "─────────────────────────────────────────";

Console.WriteLine("TiktokExplode — Test Console");
Console.WriteLine(Sep);
Console.WriteLine("  [1] Get video by URL");
Console.WriteLine("  [2] Search by keyword");
Console.WriteLine(Sep);
Console.Write("Choose (1/2): ");
var mode = Console.ReadLine()?.Trim();

Console.WriteLine(Sep);

var browserOptions = new PlaywrightFetcherOptions { BrowserChannel = "msedge" };

if (mode == "1")
{
    const string DefaultUrl = "https://www.tiktok.com/@js_nightwave/video/7579504710961548565";
    Console.Write($"Video URL (Enter for default): ");
    var url = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(url))
    {
        url = DefaultUrl;
        Console.WriteLine($"Using default: {url}");
    }

    await using var client = TiktokClient.CreateWithBrowser(browserOptions);
    await FetchAndDownloadAsync(client, url);
}
else if (mode == "2")
{
    Console.Write("Keyword: ");
    var keyword = Console.ReadLine()?.Trim();
    if (string.IsNullOrWhiteSpace(keyword))
    {
        Console.WriteLine("No keyword entered.");
        return;
    }

    Console.Write("Max results (Enter for 5): ");
    var maxStr = Console.ReadLine()?.Trim();
    var maxResults = int.TryParse(maxStr, out var n) && n > 0 ? n : 5;

    await using var searchClient = TiktokSearchClient.CreateWithBrowser(browserOptions);
    await SearchAndDownloadAsync(searchClient, keyword, maxResults);
}
else
{
    Console.WriteLine("Invalid option.");
}

// ── helpers ────────────────────────────────────────────────────────────────

static async Task FetchAndDownloadAsync(IVideoClient client, string url)
{
    const string Sep = "─────────────────────────────────────────";
    try
    {
        Console.WriteLine(Sep);
        Console.Write("Fetching video metadata...");
        var video = await client.GetVideoAsync(url);
        Console.WriteLine(" done.");
        PrintVideo(video, 1, 1);
        await PromptDownloadAsync((IDownloadClient)client, video);
    }
    catch (TiktokWafException ex)  { Console.WriteLine($"\n[WAF] {ex.Message}"); }
    catch (VideoNotFoundException ex) { Console.WriteLine($"\n[404] {ex.Message}"); }
    catch (TiktokException ex)     { Console.WriteLine($"\n[Error] {ex.Message}"); }
}

static async Task SearchAndDownloadAsync(ISearchClient client, string keyword, int maxResults)
{
    const string Sep = "─────────────────────────────────────────";
    var videos = new List<Video>();

    try
    {
        Console.Write($"Searching for \"{keyword}\"...");
        await foreach (var video in client.SearchAsync(keyword))
        {
            videos.Add(video);
            if (videos.Count >= maxResults) break;
        }
        Console.WriteLine($" found {videos.Count} result(s).");
    }
    catch (TiktokWafException ex) { Console.WriteLine($"\n[WAF] {ex.Message}"); return; }
    catch (TiktokException ex)    { Console.WriteLine($"\n[Error] {ex.Message}"); return; }

    if (videos.Count == 0)
    {
        Console.WriteLine("No results.");
        return;
    }

    Console.WriteLine(Sep);
    for (int i = 0; i < videos.Count; i++)
        PrintVideo(videos[i], i + 1, videos.Count);

    Console.Write($"\nDownload which video? (1-{videos.Count}, or Enter to skip): ");
    var pick = Console.ReadLine()?.Trim();
    if (!int.TryParse(pick, out var idx) || idx < 1 || idx > videos.Count)
    {
        Console.WriteLine("Skipping download.");
        return;
    }

    await PromptDownloadAsync(client, videos[idx - 1]);
}

static void PrintVideo(Video video, int index, int total)
{
    const string Sep = "─────────────────────────────────────────";
    Console.WriteLine(Sep);
    if (total > 1) Console.WriteLine($"  [{index}/{total}]");
    Console.WriteLine($"  ID          {video.Id}");
    Console.WriteLine($"  Author      {video.Author.Name} (@{video.Author.UniqueId})");
    Console.WriteLine($"  Duration    {video.Duration.Seconds}s");
    Console.WriteLine($"  Views       {video.Stats.Views:N0}");
    Console.WriteLine($"  Likes       {video.Stats.Likes:N0}");
    Console.WriteLine($"  Size        {video.Info.DownloadLinks.OriginalSizeInBytes / (1_024 * 1_024):F2} MB");
}

static async Task PromptDownloadAsync(IDownloadClient client, Video video)
{
    const string Sep = "─────────────────────────────────────────";
    Console.WriteLine(Sep);
    Console.Write("Download? [Y/n]: ");
    var dl = Console.ReadLine()?.Trim();
    if (!string.IsNullOrEmpty(dl) && !dl.Equals("y", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Skipped.");
        return;
    }

    Console.Write("With watermark? [Y/n]: ");
    var wm = Console.ReadLine()?.Trim();
    var isWatermarked = string.IsNullOrEmpty(wm) || wm.Equals("y", StringComparison.OrdinalIgnoreCase);

    var suffix = isWatermarked ? "watermarked" : "original";
    var fileName = $"{video.Id}_{video.Author.UniqueId}_{suffix}.mp4";
    Console.WriteLine($"Saving to: {fileName}");

    IProgress<double> progress = new Progress<double>(p => Console.Write($"\r  Downloading... {p:P0}   "));

    try
    {
        if (isWatermarked)
            await client.DownloadWatermarkedAsync(video, fileName, progress);
        else
            await client.DownloadAsync(video, fileName, progress);

        Console.WriteLine($"\n  Saved: {fileName}");
    }
    catch (TiktokException ex) { Console.WriteLine($"\n[Error] {ex.Message}"); }

    Console.WriteLine(Sep);
}
