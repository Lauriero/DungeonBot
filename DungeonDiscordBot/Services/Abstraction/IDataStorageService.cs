using System.Collections.Concurrent;

using Discord.WebSocket;

using DungeonDiscordBot.Model;
using DungeonDiscordBot.Model.Database;

namespace DungeonDiscordBot.Services.Abstraction;

public interface IDataStorageService
{
    /// <summary>
    /// Maximum number of music queries for the one guild that are saved at the database. 
    /// </summary>
    int MaxMusicQueryEntityCount { get; }

    /// <summary>
    /// Maximum number of the music collection that can belong to one discord user.
    /// </summary>
    int MaxUserFavoritesCount { get; }
    
    /// <summary>
    /// 
    /// </summary>
    ConcurrentDictionary<ulong, string> HistoryMessageSelectedOptions { get; }
    
    /// <summary>
    /// Registers new discord guild.
    /// Executed when discord bot joins a new guild.
    /// </summary>
    Task RegisterGuild(ulong guildId, string guildName, CancellationToken token = default);

    /// <summary>
    /// Removes stored guild data.
    /// </summary>
    Task UnregisterGuild(ulong guildId, CancellationToken token = default);
    
    /// <summary>
    /// Registers a discord channel and message in this channel
    /// that is used to control the music. 
    /// </summary>
    /// <exception cref="ArgumentException">When guild was not registered previously.</exception>
    Task RegisterMusicChannel(ulong guildId, SocketTextChannel musicChannel, ulong musicMessageId, 
        CancellationToken token = default);

    /// <summary>
    /// Registers a discord channel with the specified id
    /// as a channel the messages about new users will be send to.
    /// </summary>
    Task RegisterWelcomeChannel(ulong guildId, ulong channelId, CancellationToken token = default);
    
    /// <summary>
    /// Registers a discord channel with the specified id
    /// as a channel the messages about left users will be send to.
    /// </summary>
    Task RegisterRunawayChannel(ulong guildId, ulong channelId, CancellationToken token = default);

    /// <summary>
    /// Adds a text channel as a music control channel for a specified guild.
    /// Called from a bot service to put the implementation of the channel
    /// to the dictionary of channels for other services to use. 
    /// </summary>
    public void AddMusicControlChannel(ulong guildId, SocketTextChannel channel);

    /// <summary>
    /// Retrieves the music control channel implementation associated with the specified guild.
    /// </summary>
    public SocketTextChannel GetMusicControlChannel(ulong guildId);
    
    /// <summary>
    /// Gets guild data that was saved in the database.
    /// </summary>
    /// <exception cref="ArgumentException">When guild was not registered previously.</exception>
    Task<Guild> GetGuildDataAsync(ulong guildId, CancellationToken token = default);

    /// <summary>
    /// Gets guilds that has registered music channels.
    /// </summary>
    Task<List<Guild>> GetMusicGuildsAsync(CancellationToken token = default);

    /// <summary>
    /// Saves music query by putting an added music collection name
    /// and query value to the database.
    /// If the database already contains <see cref="MaxMusicQueryEntityCount"/> queries for the guild,
    /// deletes the first query and put new one on top of the list.
    /// </summary>
    Task RegisterMusicQueryAsync(ulong guildId, string queryName, string queryValue,
        CancellationToken token = default);

    /// <summary>
    /// Retrieves the last music queries
    /// that were made in the guild with the specified id.
    /// Number of queries is specified by <see cref="MaxMusicQueryEntityCount"/>
    /// </summary>
    /// <param name="guildId">Id of the guild</param>
    /// <param name="count">
    /// Count of the queries to retrieve.
    /// <see cref="MaxMusicQueryEntityCount"/> by default.
    /// </param>
    /// <param name="token">Token to cancel the query</param>
    Task<List<MusicQueryHistoryEntity>> GetLastMusicQueries(ulong guildId, int? count = null,
        CancellationToken token = default);

    /// <summary>
    /// Adds a music collection to the user favorites.
    /// </summary>
    /// <param name="userId">
    /// Id of the discord user that has requested this collection
    /// to be added to the personal favorites.
    /// </param>
    /// <param name="name">Name of the collection to be added to the user favorites.</param>
    /// <param name="query">Uri to the resource the music collection is located at.</param>
    /// <param name="token"></param>
    /// <returns>
    /// Returns result code.
    /// <see cref="AddFavoriteCollectionResult.Okay"/> if the collection has been successfully added.
    /// <see cref="AddFavoriteCollectionResult.AlreadyAdded"/> is the collection with the same query
    /// already exists as a user favorites.
    /// <see cref="AddFavoriteCollectionResult.OutOfSpace"/> if the user has reached a limit of
    /// <see cref="MaxUserFavoritesCount"/> favorites collections.
    /// </returns>
    Task<AddFavoriteCollectionResult> AddFavoriteMusicCollectionAsync(ulong userId, string name, string query,
        CancellationToken token = default);
    
    /// <summary>
    /// Gets a list of user favorites music collections.
    /// </summary>
    Task<List<FavoriteMusicCollection>> GetUserFavoriteMusicCollectionsAsync(ulong userId, CancellationToken token = default);
}