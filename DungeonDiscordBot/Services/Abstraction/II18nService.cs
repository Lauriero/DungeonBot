using DungeonDiscordBot.Model.MusicProviders;

namespace DungeonDiscordBot.Services.Abstraction;

/// <summary>
/// Provide methods required for internationalization.
/// </summary>
public interface II18nService
{
    public string GetMusicCollectionTypeName(MusicCollectionType type);
}