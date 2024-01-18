using DungeonDiscordBot.Model.Database;

namespace DungeonDiscordBot.Storage.Abstraction;

/// <summary>
/// Storage to save the last music queries.
/// </summary>
public interface IMusicHistoryStorage
{
    /// <summary>
    /// Maximum number of music queries for the one guild that are saved at the database. 
    /// </summary>
    int MaxMusicQueryEntityCount { get; }
    
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
}