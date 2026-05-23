using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Reflection;
using TiktokExplode.Bot.Configuration;
using TiktokExplode.Bot.Modules;

namespace TiktokExplode.Bot.Services;

public sealed class InteractionHandlerService(
    DiscordSocketClient client,
    InteractionService interactionService,
    IServiceProvider serviceProvider,
    IOptions<BotSettings> settings) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        client.Ready += OnReadyAsync;
        client.InteractionCreated += OnInteractionCreatedAsync;

        await interactionService.AddModulesAsync(typeof(VideoModule).Assembly, serviceProvider);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        client.Ready -= OnReadyAsync;
        client.InteractionCreated -= OnInteractionCreatedAsync;
        return Task.CompletedTask;
    }

    private async Task OnReadyAsync()
    {
        if (settings.Value.GuildId != 0)
            // Registro instantáneo — ideal para desarrollo y pruebas.
            await interactionService.RegisterCommandsToGuildAsync(settings.Value.GuildId);
        else
            // Registro global — puede tardar hasta 1 hora en propagarse.
            await interactionService.RegisterCommandsGloballyAsync();
    }

    private async Task OnInteractionCreatedAsync(SocketInteraction interaction)
    {
        var context = new SocketInteractionContext(client, interaction);
        await interactionService.ExecuteCommandAsync(context, serviceProvider);
    }
}
