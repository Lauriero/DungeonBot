using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json;

namespace DungeonDiscordBot.Model.MusicProviders.Search;

public class SearchResultEntity
{
    public string Name { get; }

    public string? Link { get; }

    public SearchResultEntity(string name, string link)
    {
        Name = name;
        Link = link;
    }
}