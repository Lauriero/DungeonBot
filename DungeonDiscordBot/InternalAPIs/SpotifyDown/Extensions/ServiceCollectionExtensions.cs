using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DungeonDiscordBot.InternalAPIs.SpotifyDown.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSpotifyDown(this IServiceCollection collection)
    {
        collection.AddSingleton<ISpotifyDownApi, SpotifyDownApi>();
        collection.RegisterDefaultDependencies();

        return collection;
    }
    
    /// <summary>
    /// Register default dependencies for the API.
    /// </summary>
    public static IServiceCollection RegisterDefaultDependencies(this IServiceCollection collection)
    {
        collection.TryAddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        collection.TryAddKeyedTransient<HttpClient>(ISpotifyDownApi.HTTP_CLIENT_SERVICE_KEY, 
            (sp, key) => new HttpClient());
        
        return collection;
    }
}