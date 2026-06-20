using Discord;
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
        interactionService.InteractionExecuted += OnInteractionExecutedAsync;

        await interactionService.AddModulesAsync(typeof(AudioModule).Assembly, serviceProvider);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        client.Ready -= OnReadyAsync;
        client.InteractionCreated -= OnInteractionCreatedAsync;
        interactionService.InteractionExecuted -= OnInteractionExecutedAsync;
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
        try
        {
            var context = new SocketInteractionContext(client, interaction);
            await interactionService.ExecuteCommandAsync(context, serviceProvider);
        }
        catch (Exception)
        {
            // Si la excepción ocurre antes de que el handler haga DeferAsync/RespondAsync,
            // el interaction quedará sin acknowledgment — intentar responder con error.
            if (!interaction.HasResponded)
                await interaction.RespondAsync("Error interno al procesar la interacción.", ephemeral: true);
        }
    }

    private async Task OnInteractionExecutedAsync(ICommandInfo? command, IInteractionContext context, IResult result)
    {
        if (result.IsSuccess) return;

        var errorMsg = result.Error switch
        {
            InteractionCommandError.UnknownCommand => "Comando no encontrado.",
            InteractionCommandError.BadArgs => "Argumentos inválidos.",
            InteractionCommandError.Exception => $"Error: {result.ErrorReason}",
            _ => result.ErrorReason
        };

        if (!context.Interaction.HasResponded)
            await context.Interaction.RespondAsync(errorMsg, ephemeral: true);
    }
}
