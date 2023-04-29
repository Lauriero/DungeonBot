using Discord.Interactions;
using Discord.WebSocket;

using DungeonDiscordBot.Controllers;
using DungeonDiscordBot.Controllers.Abstraction;
using DungeonDiscordBot.Model;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog;

namespace DungeonDiscordBot;

public class BotHostedService : IHostedService, IDisposable
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        IServiceProvider provider = CreateServiceProvider();
        foreach (MusicProvider musicProvider in MusicProvider.List) {
            await musicProvider.Value.Init();
        }
        
        await provider.GetService<IServicesAggregator>()!.Init(provider);
    }

    public async Task StopAsync(CancellationToken cancellationToken) { }

    private static IServiceProvider CreateServiceProvider()
        {
            ServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<ILogger>(InitLogger());
            serviceCollection
                .AddSingleton<InteractionService>()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<IDiscordAudioController, DiscordAudioController>()
                .AddSingleton<IDiscordBotController, DiscordBotController>()

                .AddSingleton<IVkApiController, VkApiController>()
                .AddSingleton<IServicesAggregator, ServicesAggregator>();

            return serviceCollection.BuildServiceProvider();
        }

        private static ILogger InitLogger() =>
            new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: "[{Timestamp: yyyy - MM - dd HH: mm: ss.fff zzz}] [{Level}] ({SourceContext}) {Message}{NewLine}{Exception}")
                .CreateLogger();
    
    public void Dispose()
    {
    }
}