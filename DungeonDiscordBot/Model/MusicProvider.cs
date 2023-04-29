using Ardalis.SmartEnum;

using DungeonDiscordBot.MusicProvidersControllers;

namespace DungeonDiscordBot.Model;

public class MusicProvider : SmartEnum<MusicProvider, BaseMusicProviderController>
{
    public static readonly MusicProvider VK = new MusicProvider("VK", new VkMusicProviderController());

    private MusicProvider(string name, BaseMusicProviderController value) : base(name, value) { }
}