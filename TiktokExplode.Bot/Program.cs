using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
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
            GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildVoiceStates,
            EnableVoiceDaveEncryption = true,
        }));

        Discord.LibDave.Dave.SetLogSink(new Discord.LibDave.DaveLogSinkDelegate((severity, file, line, message) =>
        {
            Console.WriteLine($"[{severity} | LIBDAVE @ {file}#{line}]: {message}");
        }));

        services.AddSingleton(provider =>
            new InteractionService(provider.GetRequiredService<DiscordSocketClient>()));

        services.AddTiktokExplode(t =>
            t.UsePlaywrightFetcher(ops => ops.BrowserChannel = "msedge"));

        services.AddMemoryCache();
        services.AddHttpClient();

        services.AddSingleton(_ => new ConcurrentDictionary<ulong, IUserMessage>());

        services.AddSingleton<MusicPlayerManager>();

        services.Configure<CloudflareR2Options>(context.Configuration.GetSection(CloudflareR2Options.Section));

        services.AddSingleton<CloudflareR2CdnProvider>();
        services.AddSingleton<ZeroXZeroCdnProvider>();
        services.AddSingleton<LitterboxCdnProvider>();

        services.AddSingleton<ICdnProvider>(x =>
            x.GetRequiredService<CloudflareR2CdnProvider>());

        services.AddSingleton<ICdnProvider>(x =>
            x.GetRequiredService<ZeroXZeroCdnProvider>());

        services.AddSingleton<ICdnProvider>(x =>
            x.GetRequiredService<LitterboxCdnProvider>());

        services.AddSingleton<CompositeCdnProvider>();

        services.AddHostedService<BotService>();
        services.AddHostedService<InteractionHandlerService>();
    }
}
