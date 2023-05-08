using Discord.Interactions;
using Discord.WebSocket;

using DungeonDiscordBot.Controllers;
using DungeonDiscordBot.Controllers.Abstraction;
using DungeonDiscordBot.Model;
using DungeonDiscordBot.MusicProvidersControllers;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog;

namespace DungeonDiscordBot;

public class BotHostedService : IHostedService, IDisposable
{
    private IServiceProvider _serviceProvider;
    public BotHostedService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (MusicProvider musicProvider in MusicProvider.List) {
            MusicProviderControllerContainer container = (MusicProviderControllerContainer) musicProvider.Value;
            container.Instance = (BaseMusicProviderController)_serviceProvider.GetRequiredService(container.ProviderType);
            await container.InitializeAsync();
        }

        foreach (IRequireInitiationService service in _serviceProvider
                     .GetServices<IRequireInitiationService>()
                     .OrderBy(s => s.InitializationPriority)) {
            await service.InitializeAsync();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (IAsyncDisposable service in _serviceProvider
                     .GetServices<IAsyncDisposable>()) {
            await service.DisposeAsync();
        }
    }

    public void Dispose()
    {
    }
}