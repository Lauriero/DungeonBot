using DungeonDiscordBot.Model.MusicProviders;
using DungeonDiscordBot.MusicProvidersControllers;

namespace DungeonDiscordBot.Utilities;

public static class UriExtensions
{
    public static BaseMusicProviderController? FindMusicProviderController(this Uri link)
    {
        foreach (MusicProvider option in MusicProvider.List) {
            BaseMusicProviderController providerController = option.Value;
            if (link.AbsoluteUri.Contains(providerController.LinksDomainName)) {
                return providerController;
            }
        }

        return null;
    }
}