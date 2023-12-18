namespace DungeonDiscordBot.Model.MusicProviders;

public class MusicSearchResponse
{
    public IEnumerable<SearchResultEntity> Result { get; set; }
    
    public MusicSearchResponse(IEnumerable<SearchResultEntity> result)
    {
        Result = result;
    }

}