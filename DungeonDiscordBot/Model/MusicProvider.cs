using System;

using Ardalis.SmartEnum;

using DungeonDiscordBot.MusicProvidersControllers;

using Microsoft.Extensions.DependencyInjection;

namespace DungeonDiscordBot.Model;

public class MusicProvider : SmartEnum<MusicProvider, BaseMusicProviderController>
{
    public static readonly MusicProvider VK = new MusicProvider("VK", typeof(VkMusicProviderController));

    public static readonly MusicProvider Yandex = new MusicProvider("Yandex", typeof(YandexMusicProviderController));

    private MusicProvider(string name, Type destinationType) 
        : base(name, new MusicProviderControllerContainer(destinationType)) { }
}

public static class MusicProvidersRegistrar
{
    public static IServiceCollection AddMusicProviders(this IServiceCollection collection)
    {
        foreach (MusicProvider musicProvider in MusicProvider.List) {
            collection.AddSingleton(musicProvider.Value.ProviderType);
        }

        return collection;
    }
}