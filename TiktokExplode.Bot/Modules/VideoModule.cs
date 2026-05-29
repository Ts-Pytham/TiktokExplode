using Discord;
using Discord.Audio;
using Discord.Interactions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using TiktokExplode.Bot.CDN;
using TiktokExplode.Bot.Handlers;
using TiktokExplode.Domain.Abstractions;
using TiktokExplode.Domain.Entities;
using TiktokExplode.Domain.Exceptions;
using TiktokExplode.Domain.ValueObjects;

namespace TiktokExplode.Bot.Modules;

[Group("tiktok", "Comandos relacionados con TikTok")]
public sealed class VideoModule(
    IVideoClient tiktok,
    IMemoryCache cache,
    ConcurrentDictionary<ulong, IAudioClient> audioClients,
    ConcurrentDictionary<ulong, IUserMessage> playerMessages,
    ConcurrentDictionary<ulong, Task> pipeTasks,
    ILogger<FFmpegAudioStream> logger,
    CloudflareR2CdnProvider provider) : InteractionModuleBase<SocketInteractionContext>
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

    [SlashCommand("audio", "Reproduce el audio de un video de TikTok en el canal de voz")]
    public async Task AudioAsync([Summary("url", "URL del video")] string url)
    {
        await DeferAsync();
        try
        {
            if (Context.User is not IVoiceState voiceState || voiceState.VoiceChannel == null)
            {
                await FollowupAsync("Debes estar en un canal de voz para usar este comando.", ephemeral: true);
                return;
            }

            var video = await tiktok.GetVideoAsync(url);
            var cdnUrl = await provider.GetUrlAsync(video.Id);

            if (cdnUrl == string.Empty)
            {
                cdnUrl = await SendVideoAsync(video, video.Id);
            }

            var guildId = Context.Guild.Id;

            // Esperar a que el piping anterior libere el stream de Discord
            if (pipeTasks.TryGetValue(guildId, out var previousTask))
            {
                try { await previousTask; } catch { }
            }

            // Conectar solo si el cliente no existe en el diccionario.
            // El evento Disconnected se encarga de removerlo cuando Discord
            // cierra la sesión por inactividad, garantizando una reconexión real.
            if (!audioClients.TryGetValue(guildId, out var audioClient))
            {
                audioClient = await voiceState.VoiceChannel.ConnectAsync();
                audioClient.Disconnected += ex =>
                {
                    audioClients.TryRemove(guildId, out var removed);
                    return Task.CompletedTask;
                };
                audioClients[guildId] = audioClient;
                // Dar tiempo a libDave para completar el handshake E2EE inicial
                await Task.Delay(1000);
            }

            var ffmpeg = new FFmpegAudioStream(cdnUrl, logger);
            await ffmpeg.StartAsync();

            var discordStream = audioClient.CreatePCMStream(AudioApplication.Mixed);

            var pipeTask = Task.Run(async () =>
            {
                try
                {
                    // Primar el stream con silencio: mantiene el estado "speaking"
                    // activo mientras FFmpeg arranca y empieza a producir datos.
                    // 1 frame PCM s16le = 20ms @ 48kHz stereo = 3840 bytes
                    var silence = new byte[3840];
                    for (int i = 0; i < 25; i++) // 25 frames = 500ms
                        await discordStream.WriteAsync(silence);

                    await ffmpeg.PipeToAsync(discordStream);
                }
                finally
                {
                    await discordStream.FlushAsync();
                    discordStream.Dispose();
                    await ffmpeg.DisposeAsync();
                }
            });

            pipeTasks[guildId] = pipeTask;

            var duration = TimeSpan.FromSeconds(video.Duration.Seconds).ToString(@"mm\:ss");

            var embed = BuildEmbed(video, voiceState, url, duration);

            if (!playerMessages.TryGetValue(guildId, out var message))
            {
                await ModifyOriginalResponseAsync(msg => { msg.Embed = embed.Build(); });

                message = await GetOriginalResponseAsync();

                playerMessages[guildId] = message;
            }
            else
            {
                await message.ModifyAsync(msg =>
                {
                    msg.Embed = embed.Build();
                });

                await DeleteOriginalResponseAsync();
            }
        }
        catch (TiktokParsingException ex) when (ex.Message.Contains("429"))
        {
            await FollowupAsync("TikTok está limitando las solicitudes (429). Espera unos segundos e intenta de nuevo.", ephemeral: true);
        }
        catch (Exception ex)
        {
            await FollowupAsync($"Error al reproducir el audio: {ex.Message}", ephemeral: true);
        }
    }

    private async Task<string> SendVideoAsync(
        Video video,
        string filename)
    {
        var streamInfo = await tiktok.DownloadAsync(video);

        using var ms = new MemoryStream();
        await streamInfo.Stream.CopyToAsync(ms);

        return await provider.UploadAsync(ms.ToArray(), filename);
    }

    private static EmbedBuilder BuildEmbed(
        Video video,
        IVoiceState voiceState,
        string url,
        string duration)
    {
        return new EmbedBuilder()
            .WithTitle($"Reproduciendo video de TikTok - Canal {voiceState.VoiceChannel.Name}")
            .WithDescription(video.Description)
            .WithUrl(url)
            .WithThumbnailUrl(video.Cover.StaticUrl)
            .AddField("Autor", video.Author.Name, true)
            .AddField("Duración", duration, true)
            .AddField("Vistas", video.Stats.Views.ToString("N0"), true)
            .AddField("Likes", video.Stats.Likes.ToString("N0"), true)
            .AddField("Compartidos", video.Stats.Shares.ToString("N0"), true)
            .WithCurrentTimestamp();
    }
}