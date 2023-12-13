using System.Collections.Concurrent;

using Discord;
using Discord.WebSocket;

using DungeonDiscordBot.Controllers.Abstraction;
using DungeonDiscordBot.Model;

namespace DungeonDiscordBot.Controllers;

public interface IUserInterfaceService : IRequireInitiationService
{
    /// <summary>
    /// Creates a new message that is used to control music
    /// and sends it to the music control channel,
    /// storing channel and message ids.
    /// </summary>
    Task CreateSongsQueueMessageAsync(ulong guildId, ConcurrentQueue<AudioQueueRecord> queue,
        MusicPlayerMetadata playerMetadata, SocketTextChannel musicChannel, CancellationToken token = default);
 
    /// <summary>
    /// Updates the current message that is used to control music.
    /// </summary>
    Task UpdateSongsQueueMessageAsync(ulong guildId, ConcurrentQueue<AudioQueueRecord> queue, 
        MusicPlayerMetadata playerMetadata, string message = "", CancellationToken token = default);
    
    MessageProperties GenerateMissingPermissionsMessage(
        string description,
        ChannelPermission[] requiredPermissions,
        SocketGuildChannel channel);
}