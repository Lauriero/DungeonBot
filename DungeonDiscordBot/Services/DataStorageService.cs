using System.Collections.Concurrent;

using Discord.WebSocket;

using DungeonDiscordBot.Model;
using DungeonDiscordBot.Model.Database;
using DungeonDiscordBot.Services.Abstraction;
using DungeonDiscordBot.Storage.Abstraction;

using Microsoft.EntityFrameworkCore;

namespace DungeonDiscordBot.Services;

/// <inheritdoc />
public class DataStorageService : IDataStorageService
{
    /// <inheritdoc />
    public IGuildsStorage Guilds { get; }
    
    /// <inheritdoc />
    public IFavoriteCollectionsStorage FavoriteCollections { get; }
    
    /// <inheritdoc />
    public IMusicHistoryStorage MusicHistory { get; }
    
    /// <inheritdoc />
    public ISelectedOptionsStorage SelectedOptions { get; }
    
    public DataStorageService(IGuildsStorage guilds, IFavoriteCollectionsStorage favoriteCollections, 
        IMusicHistoryStorage musicHistory, ISelectedOptionsStorage selectedOptions)
    {
        SelectedOptions = selectedOptions;
        FavoriteCollections = favoriteCollections;
        MusicHistory = musicHistory;
        Guilds = guilds;
    }

    

    
}