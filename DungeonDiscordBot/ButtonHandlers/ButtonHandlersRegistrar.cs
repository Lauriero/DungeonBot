using Microsoft.Extensions.DependencyInjection;

namespace DungeonDiscordBot.ButtonHandlers;

public static class ButtonHandlersRegistrar
{
    public static IServiceCollection AddButtonHandlers(this IServiceCollection collection)
    {
        collection.AddSingleton<QueueButtonHandler>();

        return collection;
    } 
}