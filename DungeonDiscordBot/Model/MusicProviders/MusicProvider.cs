using Ardalis.SmartEnum;

using DungeonDiscordBot.MusicProvidersControllers;

using Microsoft.Extensions.DependencyInjection;

namespace DungeonDiscordBot.Model.MusicProviders;

public class MusicProvider : SmartEnum<MusicProvider, BaseMusicProviderController>
{
    public static readonly MusicProvider VK = new MusicProvider("VK", typeof(VkMusicProviderController));

    public static readonly MusicProvider Yandex = new MusicProvider("Yandex", typeof(YandexMusicProviderController));

    private MusicProvider(string name, Type destinationType) 
        : base(name, new MusicProviderControllerContainer(destinationType)) { }

    /// <summary>
    /// 
    /// </summary>
    /// <exception cref="T:Ardalis.SmartEnum.SmartEnumNotFoundException"><paramref name="providerType" /> does not exist.</exception>
    /// <param name="providerType"></param>
    /// <returns></returns>
    public static MusicProvider FromProviderType(Type providerType)
    {
        foreach (MusicProvider provider in List) {
            if (provider.Value.ProviderType == providerType) {
                return provider;
            }
        }

        throw new SmartEnumNotFoundException();
    }
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