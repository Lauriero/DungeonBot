using System.Collections.Concurrent;

using Discord.WebSocket;

using DungeonDiscordBot.Model;
using DungeonDiscordBot.Model.Database;
using DungeonDiscordBot.Services.Abstraction;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DungeonDiscordBot.Services;

public class DataStorageService : IDataStorageService
{
    public int MaxMusicQueryEntityCount => 25;

    public int MaxUserFavoritesCount => 10;

    public ConcurrentDictionary<ulong, string> HistoryMessageSelectedOptions { get; } = new();

    private readonly BotDataContext _dataContext;
    private readonly ILogger<DataStorageService> _logger;
    private readonly ConcurrentDictionary<ulong, SocketTextChannel> _guildMusicChannels = new();

    public DataStorageService(BotDataContext dataContext, ILogger<DataStorageService> logger)
    {
        _dataContext = dataContext;
        _logger = logger;
    }

    public void AddMusicControlChannel(ulong guildId, SocketTextChannel channel)
    {
        _guildMusicChannels.TryAdd(guildId, channel);
        _logger.LogInformation("Registered a channel {name}@{id} as a music channel for the guild {gId}", 
            channel.Name, channel.Id, guildId);
    }

    public SocketTextChannel GetMusicControlChannel(ulong guildId)
    {
        if (!_guildMusicChannels.TryGetValue(guildId, out SocketTextChannel? channel)) {
            throw new Exception($"Attempt to get music channel with guild [{guildId}] that was not registered exists");
        }

        return channel;
    }
    
    /// <inheritdoc />
    public async Task RegisterGuild(ulong guildId, string guildName, CancellationToken token = default)
    {
        await _dataContext.Guilds.AddAsync(new Guild() {
            Id = guildId,
            Name = guildName
        }, token);
        await _dataContext.SaveChangesAsync(token);
    }
    
    /// <inheritdoc />
    public async Task UnregisterGuild(ulong guildId, CancellationToken token = default)
    {
        _dataContext.Guilds.Remove(
            await GetGuildDataAsync(guildId, token));
        await _dataContext.SaveChangesAsync(token);
    }

    /// <inheritdoc />
    public async Task RegisterMusicChannel(ulong guildId, SocketTextChannel musicChannel, ulong musicMessageId, 
        CancellationToken token = default)
    {
        Guild target = await GetGuildDataAsync(guildId, token);
        target.MusicChannelId = musicChannel.Id;
        target.MusicMessageId = musicMessageId;
        await _dataContext.SaveChangesAsync(token);
        
        AddMusicControlChannel(guildId, musicChannel);
    }

    /// <inheritdoc />
    public async Task RegisterWelcomeChannel(ulong guildId, ulong channelId, CancellationToken token = default)
    {
        Guild target = await GetGuildDataAsync(guildId, token);
        target.WelcomeChannelId = channelId;
        await _dataContext.SaveChangesAsync(token);
    }
    
    /// <inheritdoc />
    public async Task RegisterRunawayChannel(ulong guildId, ulong channelId, CancellationToken token = default)
    {
        Guild target = await GetGuildDataAsync(guildId, token);
        target.RunawayChannelId = channelId;
        await _dataContext.SaveChangesAsync(token);
    }

    /// <inheritdoc />
    public async Task<Guild> GetGuildDataAsync(ulong guildId, CancellationToken token = default)
    {
        Guild? target = await _dataContext.Guilds.FindAsync(new object?[] { guildId }, token);
        if (target is null) {
            throw new ArgumentException("This guild is not registered", nameof(guildId));
        }

        return target;
    }

    public Task<List<Guild>> GetMusicGuildsAsync(CancellationToken token = default)
    {
        return _dataContext.Guilds
            .Where(g => g.MusicChannelId.HasValue && g.MusicMessageId.HasValue)
            .ToListAsync(token);
    }

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

    public Task<List<MusicQueryHistoryEntity>> GetLastMusicQueries(ulong guildId, int? count = null, CancellationToken token = default)
    {
        return _dataContext.MusicQueries
            .Where(q => q.GuildId == guildId)
            .OrderByDescending(q => q.QueriedAt)
            .Take(count ?? MaxMusicQueryEntityCount)
            .ToListAsync(token);
    }

    public async Task<AddFavoriteCollectionResult> AddFavoriteMusicCollectionAsync(ulong userId, string name, string query,
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

    public Task<List<FavoriteMusicCollection>> GetUserFavoriteMusicCollectionsAsync(ulong userId, CancellationToken token = default)
    {
        return _dataContext.FavoriteCollections
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(token);
    }
}