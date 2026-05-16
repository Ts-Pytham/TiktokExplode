using TiktokExplode.Domain.Exceptions;
using TiktokExplode.Infrastructure.Clients;

Console.WriteLine("Ingrese la URL (Enter para usar la URL de prueba):");
var url = Console.ReadLine();

if (string.IsNullOrWhiteSpace(url))
    url = "https://www.tiktok.com/@ibaillanos/video/7638378392311663895";

await using var client = new TiktokClient();

try
{
    Console.WriteLine("Obteniendo metadatos del video...");
    var video = await client.GetVideoAsync(url);

    Console.WriteLine($"ID:          {video.Id}");
    Console.WriteLine($"Autor:       {video.Author.Name} (@{video.Author.UniqueId})");
    Console.WriteLine($"Duracion:    {video.Duration.Seconds}s");
    Console.WriteLine($"Vistas:      {video.Stats.Views}");
    Console.WriteLine($"Likes:       {video.Stats.Likes}");

    Console.WriteLine("Descargando video...");
    await using var stream = await client.DownloadWatermarkedAsync(video);
    var fileName = $"{video.Id}.mp4";
    await using var file = File.Create(fileName);
    await stream.CopyToAsync(file);
    Console.WriteLine($"Guardado: {fileName}");
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