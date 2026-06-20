using Discord;
using Discord.Interactions;
using TiktokExplode.Bot.Services;

namespace TiktokExplode.Bot.Modules;

// Sin [Group] para que los customIds coincidan exactamente: "player:pause:123", etc.
public sealed class PlayerButtonModule(MusicPlayerManager playerManager)
    : InteractionModuleBase<SocketInteractionContext>
{
    [ComponentInteraction("player:pause:*")]
    public async Task OnPauseAsync(string guildId)
    {
        await DeferAsync();
        var player = playerManager.GetPlayer(ulong.Parse(guildId));
        player?.Pause();
    }

    [ComponentInteraction("player:resume:*")]
    public async Task OnResumeAsync(string guildId)
    {
        await DeferAsync();
        var player = playerManager.GetPlayer(ulong.Parse(guildId));
        player?.Resume();
    }

    [ComponentInteraction("player:skip:*")]
    public async Task OnSkipAsync(string guildId)
    {
        await DeferAsync();
        var player = playerManager.GetPlayer(ulong.Parse(guildId));
        if (player is not null) await player.SkipAsync();
    }

    [ComponentInteraction("player:stop:*")]
    public async Task OnStopAsync(string guildId)
    {
        await DeferAsync();
        var player = playerManager.GetPlayer(ulong.Parse(guildId));
        if (player is not null) await player.StopAsync();
    }

    [ComponentInteraction("player:queue:*")]
    public async Task QueueAsync(string guildId)
    {
        var player = playerManager.GetPlayer(ulong.Parse(guildId));

        var queue = player is not null ? await player.GetQueueAsync() : [];
        if (queue.Count == 0)
        {
            await RespondAsync("La cola está vacía.", ephemeral: true);
            return;
        }

        var desc = string.Join("\n", queue.Select((q, i) => $"{i + 1}. {q.Video.Author.Name} — {q.Video.Description[..Math.Min(50, q.Video.Description.Length)]}"));
        var embed = new EmbedBuilder()
            .WithTitle("Cola de reproducción")
            .WithDescription(desc)
            .Build();

        await RespondAsync(embed: embed, ephemeral: true);
    }
}
