using DungeonDiscordBot.Model;
using DungeonDiscordBot.Model.Database;
using DungeonDiscordBot.Services;
using DungeonDiscordBot.Storage.Abstraction;

using Microsoft.EntityFrameworkCore;

namespace DungeonDiscordBot.Storage;

/// <inheritdoc />
public class FavoriteCollectionsStorage : IFavoriteCollectionsStorage
{
    /// <inheritdoc />
    public int MaxUserFavoritesCount => 10;

    private readonly BotDataContext _dataContext;
    public FavoriteCollectionsStorage(BotDataContext dataContext)
    {
        _dataContext = dataContext;
    }
    
    /// <inheritdoc />
    public async Task<AddFavoriteCollectionResult> AddAsync(ulong userId, string name, string query,
        CancellationToken token = default)
    {
        if (await _dataContext.FavoriteCollections.CountAsync() == MaxUserFavoritesCount) {
            return AddFavoriteCollectionResult.OutOfSpace;
        }

        if (await _dataContext.FavoriteCollections
                .Where(c => c.UserId == userId && c.Query == query)
                .AnyAsync()) {
            return AddFavoriteCollectionResult.AlreadyAdded;
        }

        await _dataContext.FavoriteCollections.AddAsync(new FavoriteMusicCollection {
            UserId = userId,
            CollectionName = name,
            Query = query,
            CreatedAt = DateTime.Now
        }, token);

        await _dataContext.SaveChangesAsync(token);
        return AddFavoriteCollectionResult.Okay;
    }

    /// <inheritdoc />
    public Task<List<FavoriteMusicCollection>> GetAsync(ulong userId, CancellationToken token = default)
    {
        return _dataContext.FavoriteCollections
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(token);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(ulong userId, string collectionQuery, CancellationToken token = default)
    {
        await _dataContext.FavoriteCollections
            .Where(c => c.UserId == userId && c.Query == collectionQuery)
            .ExecuteDeleteAsync(token);
        await _dataContext.SaveChangesAsync(token);
    }
}