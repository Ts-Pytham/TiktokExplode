using TiktokExplode.Domain.Exceptions;
using TiktokExplode.Infrastructure.Clients;
using TiktokExplode.Infrastructure.Common;
using TiktokExplode.Infrastructure.Options;

const string Separator = "─────────────────────────────────────────";
const string DefaultUrl = "https://www.tiktok.com/@js_nightwave/video/7579504710961548565";

Console.WriteLine("TiktokExplode — Video Downloader");
Console.WriteLine(Separator);
Console.Write("Video URL (press Enter for default): ");
var url = Console.ReadLine();

if (string.IsNullOrWhiteSpace(url))
{
    url = DefaultUrl;
    Console.WriteLine($"Using default: {url}");
}

Console.WriteLine(Separator);

await using var client = TiktokClient.CreateWithBrowser(new PlaywrightFetcherOptions
{
    BrowserChannel = "msedge"
});

try
{
    Console.Write("Fetching video metadata...");
    var video = await client.GetVideoAsync(url);
    Console.WriteLine(" done.");
    Console.WriteLine(Separator);

    Console.WriteLine($"  ID          {video.Id}");
    Console.WriteLine($"  Author      {video.Author.Name} (@{video.Author.UniqueId})");
    Console.WriteLine($"  Duration    {video.Duration.Seconds}s");
    Console.WriteLine($"  Views       {video.Stats.Views:N0}");
    Console.WriteLine($"  Likes       {video.Stats.Likes:N0}");
    Console.WriteLine($"  Size        {video.Info.DownloadLinks.OriginalSizeInBytes / (1_024 * 1_024):F2} MB");
    Console.WriteLine(Separator);

    Console.Write("Download with watermark? [Y/n]: ");
    var watermarkChoice = Console.ReadLine()?.Trim();
    var isWatermarked = string.IsNullOrEmpty(watermarkChoice) ||
                        watermarkChoice.Equals("y", StringComparison.OrdinalIgnoreCase);

    Console.Write("Download? [Y/n]: ");
    var downloadChoice = Console.ReadLine()?.Trim();
    if (!string.IsNullOrEmpty(downloadChoice) && !downloadChoice.Equals("y", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Download cancelled.");
        return;
    }

    var suffix = isWatermarked ? "watermarked" : "original";
    var fileName = $"{video.Id}_{video.Author.UniqueId}_{suffix}.mp4";

    Console.WriteLine($"Saving to: {fileName}");

    IProgress<double> progress = new Progress<double>(p =>
        Console.Write($"\r  Downloading... {p:P0}   ")
    );

    if (isWatermarked)
        await client.DownloadWatermarkedAsync(video, fileName, progress);
    else
        await client.DownloadAsync(video, fileName, progress);

    Console.WriteLine($"\n  Saved: {fileName}");
    Console.WriteLine(Separator);
}
catch (TiktokWafException ex)
{
    Console.WriteLine($"\n[WAF] Bot detection triggered: {ex.Message}");
}
catch (VideoNotFoundException ex)
{
    Console.WriteLine($"\n[404] Video not found: {ex.Message}");
}
catch (TiktokException ex)
{
    Console.WriteLine($"\n[Error] {ex.Message}");
}
