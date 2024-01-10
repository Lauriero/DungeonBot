using System.Collections.Concurrent;

using Discord.WebSocket;

using DungeonDiscordBot.Model;
using DungeonDiscordBot.Model.Database;
using DungeonDiscordBot.Storage.Abstraction;

namespace DungeonDiscordBot.Services.Abstraction;

/// <summary>
/// Service that manages different data storages.
/// </summary>
public interface IDataStorageService
{
    /// <summary>
    /// Storage section that stores data about guilds.
    /// </summary>
    public IGuildsStorage Guilds { get; }
    
    /// <summary>
    /// Storage section that stores users' favorite music collections.
    /// </summary>
    public IFavoriteCollectionsStorage FavoriteCollections { get; }
    
    /// <summary>
    /// Storage section that stores last music queries.
    /// </summary>
    public IMusicHistoryStorage MusicHistory { get; }
    
    /// <summary>
    /// Storage section that stores selected options of messages.
    /// </summary>
    ISelectedOptionsStorage SelectedOptions { get; }
}