using DungeonDiscordBot.Controllers.Abstraction;
using DungeonDiscordBot.Model;
using DungeonDiscordBot.MusicProvidersControllers;
using DungeonDiscordBot.Utilities;

using Extensions.Hosting.AsyncInitialization;

using Microsoft.Extensions.DependencyInjection;

using Serilog;
using Serilog.Templates;

namespace DungeonDiscordBot.Controllers;

public class ServicesInitializer : IAsyncInitializer
{
    private readonly IServiceProvider _serviceProvider;
    
    public ServicesInitializer(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        foreach (MusicProvider musicProvider in MusicProvider.List) {
            MusicProviderControllerContainer container = (MusicProviderControllerContainer) musicProvider.Value;
            container.Instance = (BaseMusicProviderController)_serviceProvider.GetRequiredService(container.ProviderType);
        }

        foreach (IRequireInitiationService service in _serviceProvider
                     .GetAllServices<IRequireInitiationService>()
                     .OrderBy(s => s.InitializationPriority)) {
            await service.InitializeAsync();
        }
    }
    
    //"template": "[{Timestamp: yyyy - MM - dd HH: mm: ss.fff zzz}] [{Level:u3}] ({Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)}) {Message}{NewLine}{Exception}"
}