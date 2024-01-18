using DungeonDiscordBot.Model.MusicProviders;
using DungeonDiscordBot.MusicProvidersControllers;
using DungeonDiscordBot.Services.Abstraction;
using DungeonDiscordBot.Utilities;

using Extensions.Hosting.AsyncInitialization;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DungeonDiscordBot.Services;

public class ServicesInitializer : IAsyncInitializer
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ServicesInitializer> _logger;
    
    public ServicesInitializer(IServiceProvider serviceProvider, ILogger<ServicesInitializer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
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
            
            _logger.LogInformation("Starting initialization for {service}", service.GetType().Name);
            await service.InitializeAsync();
            _logger.LogInformation("Initialization for {service} completed", service.GetType().Name);
        }
    }
    
    //"template": "[{Timestamp: yyyy - MM - dd HH: mm: ss.fff zzz}] [{Level:u3}] ({Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)}) {Message}{NewLine}{Exception}"
}