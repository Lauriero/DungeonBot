﻿using System.Net;
using System.Runtime.Versioning;

using Discord;
using Discord.Interactions;
using Discord.WebSocket;

using DungeonDiscordBot.ButtonHandlers;
using DungeonDiscordBot.Model.MusicProviders;
using DungeonDiscordBot.Services;
using DungeonDiscordBot.Services.Abstraction;
using DungeonDiscordBot.Settings;
using DungeonDiscordBot.Storage;
using DungeonDiscordBot.Storage.Abstraction;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Serilog;

using VkNet.AudioApi.Extensions;

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
                        .Configure<ProxySettings>(context.Configuration
                            .GetSection(ProxySettings.OPTIONS_SECTION_NAME))
                        .AddSingleton(sp => sp)
                        .AddLogging()
                        .AddTransient<HttpClient>(HttpClientBuilder)

                        .AddVkAudioApi()
                        .AddMusicProviders()
                        .AddButtonHandlers()

                        .AddDbContext<BotDataContext>()
                        .AddSingleton<InteractionService>()
                        .AddSingleton<DiscordSocketClient>(sp => new DiscordSocketClient(new DiscordSocketConfig {
                            GatewayIntents = GatewayIntents.All
                        }))
                        
                        .AddSingleton<IGuildsStorage, GuildsStorage>()
                        .AddSingleton<IFavoriteCollectionsStorage, FavoriteCollectionsStorage>()
                        .AddSingleton<IMusicHistoryStorage, MusicHistoryStorage>()
                        .AddSingleton<ISelectedOptionsStorage, SelectedOptionsStorage>()
                        .AddSingleton<IDataStorageService, DataStorageService>()

                        .AddSingleton<II18nService, I18nService>() 
                        .AddSingleton<IUserInterfaceService, UserInterfaceService>()
                        .AddSingleton<IDiscordAudioService, DiscordAudioService>()
                        .AddSingleton<IDiscordBotService, DiscordBotService>()
                        
                        .AddAsyncInitializer<ServicesInitializer>();
                })
                .UseConsoleLifetime()
                .Build();

            await host.InitAsync();
            await host.RunAsync();
        }

        private static HttpClient HttpClientBuilder(IServiceProvider serviceProvider)
        {
            IWebProxy? proxy = null;
            IOptions<ProxySettings>? proxySettings = serviceProvider.GetService<IOptions<ProxySettings>>();
            if (proxySettings is not null && proxySettings.Value.UseProxy) {
                ICredentials? credentials = null;
                if (proxySettings.Value.Username is not null &&
                    proxySettings.Value.Password is not null) {

                    credentials = new NetworkCredential(
                        proxySettings.Value.Username,
                        proxySettings.Value.Password);
                }

                proxy = new WebProxy {
                    Address = new Uri(proxySettings.Value.ProxyUrl),
                    BypassProxyOnLocal = false,
                    UseDefaultCredentials = false,
                    Credentials = credentials
                };
            }
            
            return new HttpClient(new HttpClientHandler { Proxy = proxy });
        }
    }
}

