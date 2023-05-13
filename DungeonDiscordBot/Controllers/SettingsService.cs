using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using DungeonDiscordBot.Model.Database;

using Microsoft.EntityFrameworkCore;

namespace DungeonDiscordBot.Controllers;

public class SettingsService : ISettingsService
{
    private readonly BotDataContext _dataContext;
    
    public SettingsService(BotDataContext dataContext)
    {
        _dataContext = dataContext;
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
        _dataContext.Guilds.Remove(new Guild {Id = guildId});
        await _dataContext.SaveChangesAsync(token);
    }

    /// <inheritdoc />
    public async Task RegisterMusicChannel(ulong guildId, ulong musicChannelId, ulong musicMessageId, 
        CancellationToken token = default)
    {
        Guild target = await GetGuildDataAsync(guildId, token);
        target.MusicChannelId = musicChannelId;
        target.MusicMessageId = musicMessageId;
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
            .ToListAsync(cancellationToken: token);
    }
}