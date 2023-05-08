using System.Collections;
using System.Reflection;

using DungeonDiscordBot.Controllers;
using DungeonDiscordBot.Controllers.Abstraction;

using Microsoft.Extensions.DependencyInjection;

namespace DungeonDiscordBot.Utilities;

public static class ServiceProviderExtensions
{
    /// <summary>
    /// Get all registered <see cref="ServiceDescriptor"/>
    /// </summary>
    /// <param name="provider"></param>
    /// <returns></returns>
    public static IEnumerable<T> GetAllServices<T>(this IServiceProvider provider)
    {
        if (provider.GetType().FullName ==
            "Microsoft.Extensions.DependencyInjection.ServiceLookup.ServiceProviderEngineScope") {
            provider = (IServiceProvider)provider.GetPropertyValue("RootProvider");
        }
        
        object site = typeof(ServiceProvider)
            .GetProperty("CallSiteFactory", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(provider)!;
        
        ServiceDescriptor[] desc = (ServiceDescriptor[])site
            .GetType()
            .GetField("_descriptors", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(site)!;

        return desc
            .Where(d => typeof(T).IsAssignableFrom(d.ServiceType))
            .Select(s => provider.GetRequiredService(s.ServiceType))
            .OfType<T>();
    }
}