using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using DungeonDiscordBot.Model.Database;

namespace DungeonDiscordBot.Controllers;

public interface ISettingsService
{
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
    Task RegisterMusicChannel(ulong guildId, ulong musicChannelId, ulong musicMessageId, 
        CancellationToken token = default);

    /// <summary>
    /// Gets guild data that was saved in the database.
    /// </summary>
    /// <exception cref="ArgumentException">When guild was not registered previously.</exception>
    Task<Guild> GetGuildDataAsync(ulong guildId, CancellationToken token = default);

    /// <summary>
    /// Gets guilds that has registered music channels.
    /// </summary>
    Task<List<Guild>> GetMusicGuildsAsync(CancellationToken token = default);
}