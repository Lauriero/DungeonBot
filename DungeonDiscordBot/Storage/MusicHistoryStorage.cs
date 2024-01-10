using DungeonDiscordBot.Model.Database;
using DungeonDiscordBot.Services;
using DungeonDiscordBot.Storage.Abstraction;

using Microsoft.EntityFrameworkCore;

namespace DungeonDiscordBot.Storage;

/// <inheritdoc />
public class MusicHistoryStorage : IMusicHistoryStorage
{
    /// <inheritdoc />
    public int MaxMusicQueryEntityCount => 25;

    private readonly BotDataContext _dataContext;
    public MusicHistoryStorage(BotDataContext dataContext)
    {
        _dataContext = dataContext;
    }
    
    /// <inheritdoc />
    public async Task RegisterMusicQueryAsync(ulong guildId, string queryName, string queryValue, CancellationToken token = default)
    {
        if (_dataContext.MusicQueries.Local.Any(q => q.GuildId == guildId && q.QueryValue == queryValue)) {
            return;
        }

        int queriesCount = _dataContext.MusicQueries.Count(q => q.GuildId == guildId);
        if (queriesCount >= MaxMusicQueryEntityCount) {
            await _dataContext.MusicQueries
                .OrderBy(q => q.QueriedAt)
                .Take(queriesCount - MaxMusicQueryEntityCount + 1)
                .ExecuteDeleteAsync(token);
        }
        
        _dataContext.MusicQueries.Add(new MusicQueryHistoryEntity() {
            GuildId = guildId,
            QueryName = queryName,
            QueryValue = queryValue,
            QueriedAt = DateTime.Now
        });

        await _dataContext.SaveChangesAsync(token);
    }

    /// <inheritdoc />
    public Task<List<MusicQueryHistoryEntity>> GetLastMusicQueries(ulong guildId, int? count = null, CancellationToken token = default)
    {
        return _dataContext.MusicQueries
            .Where(q => q.GuildId == guildId)
            .OrderByDescending(q => q.QueriedAt)
            .Take(count ?? MaxMusicQueryEntityCount)
            .ToListAsync(token);
    }
}