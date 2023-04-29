using System;
using System.Threading.Tasks;

using Discord.Interactions;
using Discord.WebSocket;

using DungeonDiscordBot.Controllers;
using DungeonDiscordBot.Controllers.Abstraction;
using DungeonDiscordBot.Model;

using Microsoft.Extensions.DependencyInjection;

using Serilog;

namespace DungeonDiscordBot
{
    internal static class Program
    {
        public static async Task Main(string[] args)
        {
            IServiceProvider provider = CreateServiceProvider(args);
            foreach (MusicProvider musicProvider in MusicProvider.List) {
                await musicProvider.Value.Init();
            }
            
            await provider.GetService<IServicesAggregator>()!.Init(provider);
            
            Console.Read();
        }

        private static IServiceProvider CreateServiceProvider(string[] args)
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
    }
}