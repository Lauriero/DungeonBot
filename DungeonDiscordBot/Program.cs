using System.Runtime.Versioning;

using Discord.Interactions;
using Discord.WebSocket;

using DungeonDiscordBot.ButtonHandlers;
using DungeonDiscordBot.Controllers;
using DungeonDiscordBot.Controllers.Abstraction;
using DungeonDiscordBot.Model;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Serilog;

namespace DungeonDiscordBot
{
    internal static class Program
    {
        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        public static async Task Main(string[] args)
        {
            IHost host = new HostBuilder()
                .UseEnvironment(Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production")
                .ConfigureAppConfiguration((hostBuilder, configBuilder) => {
                    configBuilder
                        .AddEnvironmentVariables()
                        .AddJsonFile("appsettings.json", optional: false)
                        .AddJsonFile($"appsettings.{hostBuilder.HostingEnvironment.EnvironmentName}.json",
                            optional: true);
                })
                .ConfigureLogging((context, builder) => {
                    Log.Logger = new LoggerConfiguration()
                        .ReadFrom.Configuration(context.Configuration)
                        .CreateLogger();
                    builder.AddConfiguration(context.Configuration.GetSection("Logging"));
                    builder.AddSerilog(dispose: true);
                })
                .ConfigureServices((context, services) => {
                    services
                        .Configure<AppSettings>(context.Configuration
                            .GetRequiredSection(AppSettings.OPTIONS_SECTION_NAME))
                        .AddSingleton(sp => sp)
                        .AddLogging()

                        .AddMusicProviders()
                        .AddButtonHandlers()

                        .AddDbContext<BotDataContext>()
                        .AddSingleton<InteractionService>()
                        .AddSingleton<DiscordSocketClient>()

                        .AddSingleton<IUserInterfaceService, UserInterfaceService>()
                        .AddSingleton<IDataStorageService, DataStorageService>()
                        .AddSingleton<IDiscordAudioService, DiscordAudioService>()
                        .AddSingleton<IDiscordBotService, DiscordBotService>()

                        .AddAsyncInitializer<ServicesInitializer>();
                })
                .UseConsoleLifetime()
                .Build();

            await host.InitAsync();
            await host.RunAsync();
        }
    }
}

