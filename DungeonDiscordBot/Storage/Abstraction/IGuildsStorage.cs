using Discord.WebSocket;

using DungeonDiscordBot.Model.Database;

namespace DungeonDiscordBot.Storage.Abstraction;

/// <summary>
/// Stores guilds data.
/// </summary>
public interface IGuildsStorage
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
    public void RegisterMusicChannelImpl(ulong guildId, SocketTextChannel channel);

    /// <summary>
    /// Retrieves the music control channel implementation associated with the specified guild.
    /// </summary>
    public SocketTextChannel GetMusicControlChannel(ulong guildId);
    
    /// <summary>
    /// Gets guild data that was saved in the database.
    /// </summary>
    /// <exception cref="ArgumentException">When guild was not registered previously.</exception>
    Task<Guild> GetGuildAsync(ulong guildId, CancellationToken token = default);

    /// <summary>
    /// Gets guilds that has registered music channels.
    /// </summary>
    Task<List<Guild>> GetMusicGuildsAsync(CancellationToken token = default);
}