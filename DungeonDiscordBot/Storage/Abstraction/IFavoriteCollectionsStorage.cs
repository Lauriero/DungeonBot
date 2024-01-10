using DungeonDiscordBot.Model;
using DungeonDiscordBot.Model.Database;

namespace DungeonDiscordBot.Storage.Abstraction;

/// <summary>
/// Storage to save the user favorite music collections.
/// </summary>
public interface IFavoriteCollectionsStorage
{
    /// <summary>
    /// Maximum number of the music collections that can belong to one discord user.
    /// </summary>
    int MaxUserFavoritesCount { get; }

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
    /// already exists as a user favoriteCollections.
    /// <see cref="AddFavoriteCollectionResult.OutOfSpace"/> if the user has reached a limit of
    /// <see cref="MaxUserFavoritesCount"/> favoriteCollections collections.
    /// </returns>
    Task<AddFavoriteCollectionResult> AddAsync(ulong userId, string name, string query,
        CancellationToken token = default);
    
    /// <summary>
    /// Gets a list of user favoriteCollections music collections.
    /// </summary>
    Task<List<FavoriteMusicCollection>> GetAsync(ulong userId, CancellationToken token = default);
}