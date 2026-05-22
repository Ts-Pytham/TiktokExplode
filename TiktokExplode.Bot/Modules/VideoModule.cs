using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Caching.Memory;
using TiktokExplode.Domain.Abstractions;
using TiktokExplode.Domain.Exceptions;

namespace TiktokExplode.Bot.Modules;

[Group("tiktok", "Comandos relacionados con TikTok")]
public sealed class VideoModule(IMemoryCache cache, IVideoClient tiktok) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("video", "Obten metadatos de un video de TikTok")]
    public async Task VideoAsync([Summary("url", "URL del video")] string url)
    {
        await DeferAsync();

        try
        {
            var video = await cache.GetOrCreateAsync(url, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
                return await tiktok.GetVideoAsync(url);
            });

            cache.Set(video!.Id, video, TimeSpan.FromMinutes(15));

            var duration = TimeSpan.FromSeconds(video.Duration.Seconds).ToString(@"mm\:ss");
            var builder = new EmbedBuilder()
                .WithTitle("Video de tiktok")
                .WithDescription(video.Description)
                .WithUrl(url)
                .WithThumbnailUrl(video.Cover.StaticUrl)
                .AddField("Autor", video.Author.Name, true)
                .AddField("Duración", duration, true)
                .AddField("Vistas", video.Stats.Views.ToString("N0"), true)
                .AddField("Likes", video.Stats.Likes.ToString("N0"), true)
                .AddField("Compartidos", video.Stats.Shares.ToString("N0"), true)
                .WithCurrentTimestamp();

            var components = new ComponentBuilder()
                .WithButton("Descargar original", customId: $"download:original:{video.Id}", style: ButtonStyle.Primary)
                .WithButton("Descargar con marca de agua", customId: $"download:watermark:{video.Id}", style: ButtonStyle.Secondary)
                .Build();

            await FollowupAsync(embed: builder.Build(), components: components);
        }
        catch (TiktokParsingException ex) when (ex.Message.Contains("429"))
        {
            await FollowupAsync("TikTok está limitando las solicitudes (429). Espera unos segundos e intenta de nuevo.", ephemeral: true);
        }
        catch (Exception ex)
        {
            await FollowupAsync($"Error al obtener el video: {ex.Message}", ephemeral: true);
        }
    }
}