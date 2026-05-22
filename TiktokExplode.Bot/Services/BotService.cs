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
    ILogger<BotService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        client.Log += OnLog;

        await client.LoginAsync(TokenType.Bot, settings.Value.Token);
        await client.StartAsync();
    }

    private async Task OnLog(LogMessage msg)
    {
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
