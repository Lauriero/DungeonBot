using DungeonDiscordBot.Model.MusicProviders;

namespace DungeonDiscordBot.Controllers.Abstraction;

public interface IMusicProviderService
{
    Task<MusicCollectionResponse?> FetchAudios(Uri audiosUri, int quantity = -1);
}