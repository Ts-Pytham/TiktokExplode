using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TiktokExplode.Bot.CDN;
using TiktokExplode.Bot.Configuration;
using TiktokExplode.Bot.Services;
using TiktokExplode.Extensions.DependencyInjection;

namespace TiktokExplode.Bot;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(ConfigureServices)
            .Build();

        await host.RunAsync();
    }

    private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        services.Configure<BotSettings>(context.Configuration.GetSection("BotSettings"));

        services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds
        }));

        services.AddSingleton(provider =>
            new InteractionService(provider.GetRequiredService<DiscordSocketClient>()));

        services.AddTiktokExplode(t =>
            t.UsePlaywrightFetcher(ops => ops.BrowserChannel = "msedge"));

        services.AddMemoryCache();
        services.AddHttpClient();

        // CDN providers — se prueban en orden: R2 → 0x0.st → Litterbox
        services.Configure<CloudflareR2Options>(context.Configuration.GetSection(CloudflareR2Options.Section));
        services.AddSingleton<ICdnProvider, CloudflareR2CdnProvider>();
        services.AddSingleton<ICdnProvider, ZeroXZeroCdnProvider>();
        services.AddSingleton<ICdnProvider, LitterboxCdnProvider>();
        services.AddSingleton<CompositeCdnProvider>();

        services.AddHostedService<BotService>();
        services.AddHostedService<InteractionHandlerService>();
    }
}
