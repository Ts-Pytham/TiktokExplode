using Discord.Interactions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TiktokExplode.Bot.CDN;
using TiktokExplode.Domain.Abstractions;
using TiktokExplode.Domain.Entities;

namespace TiktokExplode.Bot.Modules;

public sealed class DownloadModule(
    IVideoClient tiktok,
    IMemoryCache cache,
    ILogger<DownloadModule> logger,
    CompositeCdnProvider cdnProvider) : InteractionModuleBase<SocketInteractionContext>
{
    [ComponentInteraction("download:original:*")]
    public async Task DownloadOriginalAsync(string videoId)
    {
        await DeferAsync();

        if (!cache.TryGetValue(videoId, out Video? video))
        {
            await FollowupAsync("El enlace expiro, usa el comando nuevamente.", ephemeral: true);
            return;
        }

        try
        {
            await using var streamInfo = await tiktok.DownloadAsync(video!);
            await SendVideoAsync(ms =>
                Context.Channel.SendFileAsync(ms, $"{video!.Id}.mp4"), streamInfo, $"{video!.Id}.mp4");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error descargando original {VideoId}", videoId);
            await FollowupAsync($"Error al descargar: {ex.Message}", ephemeral: true);
        }
    }

    [ComponentInteraction("download:watermark:*")]
    public async Task DownloadWatermarkedAsync(string videoId)
    {
        await DeferAsync();

        if (!cache.TryGetValue(videoId, out Video? video))
        {
            await FollowupAsync("El enlace expiro, usa el comando nuevamente.", ephemeral: true);
            return;
        }

        try
        {
            await using var streamInfo = await tiktok.DownloadWatermarkedAsync(video!);
            await SendVideoAsync(ms =>
                Context.Channel.SendFileAsync(ms, $"{video!.Id}_watermark.mp4"), streamInfo, $"{video!.Id}_watermark.mp4");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error descargando watermark {VideoId}", videoId);
            await FollowupAsync($"Error al descargar: {ex.Message}", ephemeral: true);
        }
    }

    private const int DiscordMaxBytes = 8 * 1024 * 1024; // 8 MB — límite oficial para bots

    private async Task SendVideoAsync(
        Func<MemoryStream, Task> discordUpload, 
        Domain.ValueObjects.StreamInfo streamInfo, 
        string filename)
    {
        byte[] data;
        using (var tmp = new MemoryStream())
        {
            await streamInfo.Stream.CopyToAsync(tmp);
            data = tmp.ToArray();
        }

        logger.LogInformation("Video listo: {Size} bytes ({SizeMb:F1} MB)", data.Length, data.Length / (1024.0 * 1024.0));

        if (data.Length <= DiscordMaxBytes)
        {
            try
            {
                using var discordStream = new MemoryStream(data, writable: false);
                await discordUpload(discordStream);
                await DeleteOriginalResponseAsync();
                return;
            }
            catch (Exception discordEx)
            {
                logger.LogWarning(discordEx, "Discord upload falló ({Size} bytes), usando CDN", data.Length);
            }
        }
        else
        {
            logger.LogInformation("Video supera el límite de Discord ({SizeMb:F1} MB), subiendo directo al CDN", data.Length / (1024.0 * 1024.0));
        }

        try
        {
            var url = await cdnProvider.UploadAsync(data, filename);
            await FollowupAsync(url);
        }
        catch (Exception cdnEx)
        {
            logger.LogError(cdnEx, "Todos los providers CDN fallaron");
            await FollowupAsync("No se pudo subir el video. Intenta de nuevo más tarde.", ephemeral: true);
        }
    }
}