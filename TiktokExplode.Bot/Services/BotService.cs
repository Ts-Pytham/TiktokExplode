using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TiktokExplode.Bot.Configuration;

namespace TiktokExplode.Bot.Services;

public sealed class BotService(
    DiscordSocketClient client,
    IOptions<BotSettings> settings,
    MusicPlayerManager manager,
    ILogger<BotService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        client.Log += OnLog;
        client.UserVoiceStateUpdated += async (user, before, after) =>
        {
            if (before.VoiceChannel is null)
                return;

            var channel = before.VoiceChannel;
            var guild = channel.Guild;

            var botUser = guild.GetUser(client.CurrentUser.Id);
            if (botUser?.VoiceChannel?.Id != channel.Id)
                return;

            var humanUsers = channel.Users.Count(u => !u.IsBot);
            if (humanUsers == 0)
            {
                logger.LogInformation("Bot solo en {Channel}, iniciando desconexión en 10s", channel.Name);
                await Task.Delay(TimeSpan.FromSeconds(10));

                humanUsers = channel.Users.Count(u => !u.IsBot);
                if (humanUsers == 0)
                    await manager.HandleVoiceStateAsync(guild.Id);
            }
        };

        await client.LoginAsync(TokenType.Bot, settings.Value.Token);
        await client.StartAsync();
    }

    private async Task OnLog(LogMessage msg)
    {
        // Ignorar warnings de descifrado DAVE — el bot no necesita descifrar
        // audio entrante de usuarios; estos warnings son inofensivos.
        if (msg.Source is "Dave" || (msg.Message?.Contains("LIBDAVE", StringComparison.OrdinalIgnoreCase) ?? false))
        {
            await Task.CompletedTask;
            return;
        }

        logger.LogInformation("[{Severity}] {Source}: {Message}", msg.Severity, msg.Source, msg.Message);
        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await client.StopAsync();
    }

    public async Task LogoutAsync()
    {
        await client.LogoutAsync();
    }
}
