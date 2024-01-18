using DungeonDiscordBot.Model.MusicProviders;
using DungeonDiscordBot.Services.Abstraction;

namespace DungeonDiscordBot.Services;

public class I18nService : II18nService
{
    public string GetMusicCollectionTypeName(MusicCollectionType type)
    {
        return type.ToString().ToLower();
    }
}