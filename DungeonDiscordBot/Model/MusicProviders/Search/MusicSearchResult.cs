namespace DungeonDiscordBot.Model.MusicProviders.Search;

public class MusicSearchResult
{
    public MusicProvider Provider { get; }
    
    public IEnumerable<SearchResultEntity> Entities { get; }
    
    public MusicSearchResult(MusicProvider provider, IEnumerable<SearchResultEntity> entities)
    {
        Provider = provider;
        Entities = entities;
    }

}