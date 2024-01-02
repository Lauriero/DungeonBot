using DungeonDiscordBot.Controllers.Abstraction;
using DungeonDiscordBot.Model.MusicProviders;
using DungeonDiscordBot.MusicProvidersControllers;
using DungeonDiscordBot.Utilities;

namespace DungeonDiscordBot.Controllers;

public class MusicProviderService : IMusicProviderService
{
    public async Task<MusicCollectionResponse?> FetchAudios(Uri audiosUri, int quantity = -1)
    {
        BaseMusicProviderController? controller = audiosUri.FindMusicProviderController();
        if (controller is null) {
            return null;
        }

        return await controller.GetAudiosFromLinkAsync(audiosUri, quantity);
    }
}