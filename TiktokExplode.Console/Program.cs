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
        await PromptDownloadVideoAsync(client, video);
    }
    catch (TiktokWafException ex)  { Console.WriteLine($"\n[WAF] {ex.Message}"); }
    catch (VideoNotFoundException ex) { Console.WriteLine($"\n[404] {ex.Message}"); }
    catch (TiktokException ex)     { Console.WriteLine($"\n[Error] {ex.Message}"); }
}

static async Task SearchAndDownloadAsync(ISearchClient client, string keyword, int maxResults)
{
    const string Sep = "─────────────────────────────────────────";
    var mediaList = new List<Media>();

    try
    {
        Console.Write($"Searching for \"{keyword}\"...");
        await foreach (var media in client.SearchAsync(keyword))
        {
            mediaList.Add(media);
            if (mediaList.Count >= maxResults) break;
        }
        Console.WriteLine($" found {mediaList.Count} result(s).");
    }
    catch (TiktokWafException ex) { Console.WriteLine($"\n[WAF] {ex.Message}"); return; }
    catch (TiktokException ex)    { Console.WriteLine($"\n[Error] {ex.Message}"); return; }

    if (mediaList.Count == 0)
    {
        Console.WriteLine("No results.");
        return;
    }

    Console.WriteLine(Sep);
    for (int i = 0; i < mediaList.Count; i++)
    {
        var media = mediaList[i];
        if(media is Video video)
            PrintVideo(video, i + 1, mediaList.Count);
        else if (media is Carousel carousel)
            PrintCarousel(carousel, i + 1, mediaList.Count);
    }
        

    Console.Write($"\nDownload which video? (1-{mediaList.Count}, or Enter to skip): ");
    var pick = Console.ReadLine()?.Trim();
    if (!int.TryParse(pick, out var idx) || idx < 1 || idx > mediaList.Count)
    {
        Console.WriteLine("Skipping download.");
        return;
    }
    if (mediaList[idx - 1] is Video video2)
        await PromptDownloadVideoAsync(client, video2);
    else if (mediaList[idx - 1] is Carousel carousel2)
        await PromptDownloadCarouselAsync(client, carousel2);
}

static void PrintVideo(Video video, int index, int total)
{
    const string Sep = "─────────────────────────────────────────";
    Console.WriteLine(Sep);
    if (total > 1) Console.WriteLine($"  [{index}/{total}]");
    Console.WriteLine($"  Type        Video");
    Console.WriteLine($"  URL         {video.Url}");
    Console.WriteLine($"  ID          {video.Id}");
    Console.WriteLine($"  Author      {video.Author.Name} (@{video.Author.UniqueId})");
    Console.WriteLine($"  Duration    {video.Duration.Seconds}s");
    Console.WriteLine($"  Views       {video.Stats.Views:N0}");
    Console.WriteLine($"  Likes       {video.Stats.Likes:N0}");
    Console.WriteLine($"  Size        {video.Info.DownloadLinks.OriginalSizeInBytes / (1_024 * 1_024):F2} MB");
}

static void PrintCarousel(Carousel carousel, int index, int total)
{
    const string Sep = "─────────────────────────────────────────";
    Console.WriteLine(Sep);
    if (total > 1) Console.WriteLine($"  [{index}/{total}]");
    Console.WriteLine($"  Type        Carousel");
    Console.WriteLine($"  URL         {carousel.Url}");
    Console.WriteLine($"  ID          {carousel.Id}");
    Console.WriteLine($"  Author      {carousel.Author.Name} (@{carousel.Author.UniqueId})");
    Console.WriteLine($"  Views       {carousel.Stats.Views:N0}");
    Console.WriteLine($"  Likes       {carousel.Stats.Likes:N0}");
    Console.WriteLine($"  Carousel items: {carousel.Post.Images.Count}");
}

static async Task PromptDownloadVideoAsync(IDownloadClient client, Video video)
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

static async Task PromptDownloadCarouselAsync(
    IDownloadClient client,
    Carousel carousel)
{
    const string Sep = "─────────────────────────────────────────";

    Console.WriteLine(Sep);

    Console.Write("Download images? [Y/n]: ");
    var dl = Console.ReadLine()?.Trim();

    if (!string.IsNullOrEmpty(dl) &&
        !dl.Equals("y", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Skipped.");
        return;
    }

    var directoryName =
        $"{carousel.Id}_{carousel.Author.UniqueId}_images";

    Console.WriteLine($"Saving to: {directoryName}");

    IProgress<double> progress =
        new Progress<double>(p =>
            Console.Write($"\r  Downloading... {p:P0}   "));

    try
    {
        await client.DownloadCarouselImagesAsync(
            carousel,
            directoryName,
            progress);

        Console.WriteLine($"\n  Saved: {directoryName}");
    }
    catch (TiktokException ex)
    {
        Console.WriteLine($"\n[Error] {ex.Message}");
    }

    Console.WriteLine(Sep);
}