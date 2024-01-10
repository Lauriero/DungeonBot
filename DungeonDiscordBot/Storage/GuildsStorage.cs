using System.Collections.Concurrent;

using Discord.WebSocket;

using DungeonDiscordBot.Model.Database;
using DungeonDiscordBot.Services;
using DungeonDiscordBot.Services.Abstraction;
using DungeonDiscordBot.Storage.Abstraction;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DungeonDiscordBot.Storage;

/// <inheritdoc />
public class GuildsStorage : IGuildsStorage
{
    private readonly BotDataContext _dataContext;
    private readonly ILogger<GuildsStorage> _logger;
    private readonly ConcurrentDictionary<ulong, SocketTextChannel> _guildMusicChannels = new();
    
    public GuildsStorage(BotDataContext dataContext, ILogger<GuildsStorage> logger)
    {
        _dataContext = dataContext;
        _logger = logger;
    }

    #region Guild data
    
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
        await _dataContext.Guilds
            .Where(g => g.Id == guildId)
            .ExecuteDeleteAsync(token);
        await _dataContext.SaveChangesAsync(token);
    }
    
    /// <inheritdoc />
    public async Task<Guild> GetGuildAsync(ulong guildId, CancellationToken token = default)
    {
        Guild? target = await _dataContext.Guilds.FindAsync(new object?[] { guildId }, token);
        if (target is null) {
            throw new ArgumentException("This guild is not registered", nameof(guildId));
        }

        return target;
    }
    
    /// <inheritdoc />
    public Task<List<Guild>> GetMusicGuildsAsync(CancellationToken token = default)
    {
        return _dataContext.Guilds
            .Where(g => g.MusicChannelId.HasValue && g.MusicMessageId.HasValue)
            .ToListAsync(token);
    }

    #endregion

    #region Music control channel

    /// <inheritdoc />
    public async Task RegisterMusicChannel(ulong guildId, SocketTextChannel musicChannel, ulong musicMessageId, 
        CancellationToken token = default)
    {
        Guild target = await GetGuildAsync(guildId, token);
        target.MusicChannelId = musicChannel.Id;
        target.MusicMessageId = musicMessageId;
        await _dataContext.SaveChangesAsync(token);
        
        RegisterMusicChannelImpl(guildId, musicChannel);
    }
    
    /// <inheritdoc />
    public void RegisterMusicChannelImpl(ulong guildId, SocketTextChannel channel)
    {
        _guildMusicChannels.TryAdd(guildId, channel);
        _logger.LogInformation("Registered a channel {name}@{id} as a music channel for the guild {gId}", 
            channel.Name, channel.Id, guildId);
    }
    
    /// <inheritdoc />
    public SocketTextChannel GetMusicControlChannel(ulong guildId)
    {
        if (!_guildMusicChannels.TryGetValue(guildId, out SocketTextChannel? channel)) {
            throw new Exception($"Attempt to get music channel with guild [{guildId}] that was not registered exists");
        }

        return channel;
    }

    #endregion

    #region Info channels

    /// <inheritdoc />
    public async Task RegisterWelcomeChannel(ulong guildId, ulong channelId, CancellationToken token = default)
    {
        Guild target = await GetGuildAsync(guildId, token);
        target.WelcomeChannelId = channelId;
        await _dataContext.SaveChangesAsync(token);
    }
    
    /// <inheritdoc />
    public async Task RegisterRunawayChannel(ulong guildId, ulong channelId, CancellationToken token = default)
    {
        Guild target = await GetGuildAsync(guildId, token);
        target.RunawayChannelId = channelId;
        await _dataContext.SaveChangesAsync(token);
    }
    
    #endregion
}