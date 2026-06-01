using Discord;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using TiktokExplode.Bot.CDN;
using TiktokExplode.Bot.Models;
using TiktokExplode.Domain.Abstractions;
using TiktokExplode.Domain.Entities;

namespace TiktokExplode.Bot.Services;

public sealed class MusicPlayerManager(
    IVideoClient tiktok,
    ILogger<MusicPlayer> playerLogger,
    CloudflareR2CdnProvider provider)
{
    private readonly ConcurrentDictionary<ulong, MusicPlayer> _players = new();

    public async Task<QueueItem> EnqueueAsync(
        ulong guildId,
        IVoiceChannel voiceChannel,
        string url,
        Func<QueueItem, Task>? onTrackStarted = null,
        Func<Task>? onQueueEmpty = null)
    {
        var video = await tiktok.GetVideoAsync(url);

        var cdnUrl = await provider.GetUrlAsync(video.Id);

        if (cdnUrl == string.Empty)
        {
            cdnUrl = await SendVideoAsync(video, video.Id);
        }

        var item = new QueueItem(cdnUrl, url, video);

        var player = await GetOrCreateAsync(guildId, voiceChannel, onTrackStarted, onQueueEmpty);
        await player.EnqueueAsync(item);

        return item;
    }

    public MusicPlayer? GetPlayer(ulong guildId)
        => _players.GetValueOrDefault(guildId);

    private readonly SemaphoreSlim _createLock = new(1, 1);

    private async Task<MusicPlayer> GetOrCreateAsync(
        ulong guildId,
        IVoiceChannel voiceChannel,
        Func<QueueItem, Task>? onTrackStarted = null,
        Func<Task>? onQueueEmpty = null)
    {
        if (_players.TryGetValue(guildId, out var existingPlayer))
            return existingPlayer;

        await _createLock.WaitAsync();
        try
        {
            if (_players.TryGetValue(guildId, out existingPlayer))
                return existingPlayer;

            var player = new MusicPlayer(voiceChannel, guildId, playerLogger);

            if (onTrackStarted is not null)
                player.OnTrackStarted += onTrackStarted;

            if (onQueueEmpty is not null)
                player.OnQueueEmpty += onQueueEmpty;

            // Eliminar de _players solo cuando el player se destruya completamente
            player.OnDisposed += () =>
            {
                _players.TryRemove(guildId, out _);
                return Task.CompletedTask;
            };

            await player.StartAsync();
            _players[guildId] = player;

            return player;
        }
        finally
        {
            _createLock.Release();
        }
    }

    public async Task HandleVoiceStateAsync(ulong guildId)
    {
        if (_players.TryGetValue(guildId, out var player))
            await player.DisposeAsync();
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
}