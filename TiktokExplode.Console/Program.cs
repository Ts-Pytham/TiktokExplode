using TiktokExplode.Domain.Exceptions;
using TiktokExplode.Infrastructure.Clients;
using TiktokExplode.Infrastructure.Common;
using TiktokExplode.Infrastructure.Options;

Console.WriteLine("Ingrese la URL (Enter para usar la URL de prueba):");
var url = Console.ReadLine();

if (string.IsNullOrWhiteSpace(url))
    url = "https://www.tiktok.com/@js_nightwave/video/7579504710961548565";

await using var client = TiktokClient.CreateWithBrowser(new PlaywrightFetcherOptions
{
    BrowserChannel = "msedge"
});

try
{
    Console.WriteLine("Obteniendo metadatos del video...");
    var video = await client.GetVideoAsync(url);

    Console.WriteLine($"ID:          {video.Id}");
    Console.WriteLine($"Autor:       {video.Author.Name} (@{video.Author.UniqueId})");
    Console.WriteLine($"Duracion:    {video.Duration.Seconds}s");
    Console.WriteLine($"Vistas:      {video.Stats.Views}");
    Console.WriteLine($"Likes:       {video.Stats.Likes}");
    Console.WriteLine($"Peso:        {video.Info.DownloadLinks.OriginalSizeInBytes} B");

    Console.WriteLine("¿Desea descargar el video? (s/n)");
    string downloadChoice = Console.ReadLine() ?? "";
    if (string.IsNullOrEmpty(downloadChoice) || !downloadChoice.Trim().Equals("s", StringComparison.CurrentCultureIgnoreCase))
    {
        Console.WriteLine("Descarga cancelada.");
        return;
    }

    Console.WriteLine("Descargando video...");
    var isWatermarked = true;

    var fileName = $"{video.Id}_{video.Author.Name}_{(isWatermarked is true ? "watermarked" : "original")}.mp4";
    IProgress<double> progress = new Progress<double>(p => Console.Write($"\rProgreso: {p:P0}   "));
    await client.DownloadAsync(video, fileName, progress);
    Console.WriteLine($"\nGuardado: {fileName}");
}
catch (TiktokWafException ex)
{
    Console.WriteLine($"WAF detectado: {ex.Message}");
}
catch (VideoNotFoundException ex)
{
    Console.WriteLine($"Video no encontrado: {ex.Message}");
}
catch (TiktokException ex)
{
    Console.WriteLine($"Error de TikTok: {ex.Message}");
}
