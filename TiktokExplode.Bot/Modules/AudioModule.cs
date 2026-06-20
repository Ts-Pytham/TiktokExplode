using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using TiktokExplode.Bot.Models;
using TiktokExplode.Bot.Services;
using TiktokExplode.Domain.Abstractions;
using TiktokExplode.Domain.Exceptions;

namespace TiktokExplode.Bot.Modules;

[Group("tiktok", "Comandos relacionados con TikTok")]
public sealed class AudioModule(
    MusicPlayerManager playerManager,
    ConcurrentDictionary<ulong, IUserMessage> playerMessages,
    IVideoClient tiktok,
    IMemoryCache cache) : InteractionModuleBase<SocketInteractionContext>
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

    [SlashCommand("play", "Reproduce el audio de un video de TikTok en el canal de voz")]
    public async Task AudioAsync([Summary("url")] string url)
    {
        await DeferAsync();
        try
        {
            if (Context.User is not IVoiceState { VoiceChannel: not null } voiceState)
            {
                await FollowupAsync("Debes estar en un canal de voz.", ephemeral: true);
                return;
            }

            var guildId = Context.Guild.Id;
            var textChannel = Context.Channel;
            var channelName = voiceState.VoiceChannel.Name;

            var item = await playerManager.EnqueueAsync(
                guildId,
                voiceState.VoiceChannel,
                url,
                onTrackStarted: async trackItem =>
                {
                    var components = new ComponentBuilder()
                        .WithButton("⏸ Pausa", customId: $"player:pause:{guildId}", style: ButtonStyle.Secondary)
                        .WithButton("▶ Reanudar", customId: $"player:resume:{guildId}", style: ButtonStyle.Success)
                        .WithButton("⏭ Skip", customId: $"player:skip:{guildId}", style: ButtonStyle.Primary)
                        .WithButton("⏹ Stop", customId: $"player:stop:{guildId}", style: ButtonStyle.Danger)
                        .WithButton("📋 Cola", customId: $"player:queue:{guildId}", style: ButtonStyle.Secondary)
                        .Build();

                    var dur = TimeSpan.FromSeconds(trackItem.Video.Duration.Seconds).ToString(@"mm\:ss");
                    var embed = BuildEmbed(trackItem, channelName, dur);
                    if (!playerMessages.TryGetValue(guildId, out var msg))
                    {
                        msg = await textChannel.SendMessageAsync(
                            embed: embed.Build(),
                            components: components);
                        playerMessages[guildId] = msg;
                    }
                    else
                        await msg.ModifyAsync(m =>
                        {
                            m.Embed = embed.Build();
                            m.Components = components;
                        });
                },
                onQueueEmpty: () =>
                {
                    playerMessages.TryRemove(guildId, out _);
                    return Task.CompletedTask;
                });

            var player = playerManager.GetPlayer(guildId);
            var queue = player is not null ? await player.GetQueueAsync() : [];
            var position = queue.Count;

            var desc = item.Video.Description.Length > 0
                ? item.Video.Description[..Math.Min(40, item.Video.Description.Length)]
                : string.Empty;
            var response = position == 0
                ? $"▶ Reproduciendo: **{item.Video.Author.Name}** — {desc}"
                : $"🎵 Añadido a la cola (posición **{position}**): **{item.Video.Author.Name}** — {desc}";

            await FollowupAsync(response, ephemeral: true);
        }
        catch (TiktokParsingException ex) when (ex.Message.Contains("429"))
        {
            await FollowupAsync("TikTok está limitando las solicitudes (429).", ephemeral: true);
        }
        catch (Exception ex)
        {
            await FollowupAsync($"Error: {ex.Message}", ephemeral: true);
        }
    }

    [SlashCommand("skip", "Salta la canción actual")]
    public async Task SkipAsync()
    {
        var player = playerManager.GetPlayer(Context.Guild.Id);
        if (player is null) { await RespondAsync("No hay nada reproduciéndose.", ephemeral: true); return; }
        await player.SkipAsync();
        await RespondAsync("Saltando...", ephemeral: true);
    }

    [SlashCommand("stop", "Detiene la reproducción y vacía la cola")]
    public async Task StopAsync()
    {
        var player = playerManager.GetPlayer(Context.Guild.Id);
        if (player is null) { await RespondAsync("No hay nada reproduciéndose.", ephemeral: true); return; }
        await player.StopAsync();
        await RespondAsync("Detenido.", ephemeral: true);
    }

    [SlashCommand("pause", "Pausa la reproducción")]
    public async Task PauseAsync()
    {
        var player = playerManager.GetPlayer(Context.Guild.Id);
        if (player is null) { await RespondAsync("No hay nada reproduciéndose.", ephemeral: true); return; }
        player.Pause();
        await RespondAsync("Pausado.", ephemeral: true);
    }

    [SlashCommand("resume", "Reanuda la reproducción")]
    public async Task ResumeAsync()
    {
        var player = playerManager.GetPlayer(Context.Guild.Id);
        if (player is null) { await RespondAsync("No hay nada reproduciéndose.", ephemeral: true); return; }
        player.Resume();
        await RespondAsync("Reanudado.", ephemeral: true);
    }

    [SlashCommand("queue", "Muestra la cola de reproducción")]
    public async Task QueueAsync()
    {
        var player = playerManager.GetPlayer(Context.Guild.Id);
        if (player is null) { await RespondAsync("La cola está vacía.", ephemeral: true); return; }

        var queue = await player.GetQueueAsync();
        if (queue.Count == 0) { await RespondAsync("La cola está vacía.", ephemeral: true); return; }

        var desc = string.Join("\n", queue.Select((q, i) => $"{i + 1}. {q.Video.Author.Name} — {q.Video.Description[..Math.Min(50, q.Video.Description.Length)]}"));
        var embed = new EmbedBuilder()
            .WithTitle("Cola de reproducción")
            .WithDescription(desc)
            .Build();

        await RespondAsync(embed: embed);
    }

    private static EmbedBuilder BuildEmbed(
        QueueItem item,
        string channelName,
        string duration)
    {
        return new EmbedBuilder()
            .WithTitle($"Reproduciendo video de TikTok - Canal {channelName}")
            .WithDescription(item.Video.Description)
            .WithUrl(item.OriginalUrl)
            .WithThumbnailUrl(item.Video.Cover.StaticUrl)
            .AddField("Autor", item.Video.Author.Name, true)
            .AddField("Duración", duration, true)
            .AddField("Vistas", item.Video.Stats.Views.ToString("N0"), true)
            .AddField("Likes", item.Video.Stats.Likes.ToString("N0"), true)
            .AddField("Compartidos", item.Video.Stats.Shares.ToString("N0"), true)
            .WithCurrentTimestamp();
    }
}