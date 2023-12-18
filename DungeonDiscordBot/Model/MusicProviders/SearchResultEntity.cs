namespace DungeonDiscordBot.Model.MusicProviders;

public class SearchResultEntity
{
    public string Name { get; }
    
    public string Link { get; }
    
    public SearchResultEntity(string name, string link)
    {
        Name = name;
        Link = link;
    }
}