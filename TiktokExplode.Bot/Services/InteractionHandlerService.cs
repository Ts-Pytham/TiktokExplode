using System.Reflection;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;

namespace TiktokExplode.Bot.Services;

public sealed class InteractionHandlerService(
    DiscordSocketClient client,
    InteractionService interactionService,
    IServiceProvider serviceProvider) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        client.Ready += OnReadyAsync;
        client.InteractionCreated += OnInteractionCreatedAsync;

        await interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), serviceProvider);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        client.Ready -= OnReadyAsync;
        client.InteractionCreated -= OnInteractionCreatedAsync;
        return Task.CompletedTask;
    }

    private async Task OnReadyAsync()
    {
        await interactionService.RegisterCommandsGloballyAsync();
    }

    private async Task OnInteractionCreatedAsync(SocketInteraction interaction)
    {
        var context = new SocketInteractionContext(client, interaction);
        await interactionService.ExecuteCommandAsync(context, serviceProvider);
    }
}
