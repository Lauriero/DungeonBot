using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

using Discord.WebSocket;

using DungeonDiscordBot.Model;

namespace DungeonDiscordBot.Controllers.Abstraction;

public interface IDiscordAudioController
{
    Task Init(IServicesAggregator aggregator);

    /// <summary>
    /// Adds a song to the queue.
    /// </summary>
    /// <param name="guildId">Id of the server.</param>
    /// <param name="audio">Song data.</param>
    void AddAudio(ulong guildId, AudioQueueRecord audio);

    /// <summary>
    /// Adds songs to the queue.
    /// </summary>
    /// <param name="guildId">Id of the server.</param>
    /// <param name="audios">Song data.</param>
    void AddAudios(ulong guildId, IEnumerable<AudioQueueRecord> audios);

    /// <summary>
    /// Starts playing the queue.
    /// </summary>
    /// <param name="guildId">Id of the server.</param>
    Task PlayQueueAsync(ulong guildId);

    /// <summary>
    /// Stops playing the queue.
    /// </summary>
    /// <param name="guildId">Id of the server.</param>
    Task StopQueueAsync(ulong guildId);

    /// <summary>
    /// Remove all songs from the queue.
    /// </summary>
    /// <param name="guildId">Id of the server.</param>
    void ClearQueue(ulong guildId);

    /// <summary>
    /// Registers a discord voice channel that will be used as a speaking channel on this server.
    /// </summary>
    /// <param name="guild">Server instance.</param>
    /// <param name="channelId">Id of the channel.</param>
    void RegisterChannel(SocketGuild guild, ulong channelId);

    /// <summary>
    /// Gets the current queue of the server. 
    /// </summary>
    /// <returns>Queue.</returns>
    ConcurrentQueue<AudioQueueRecord> GetQueue(ulong guildId);

    /// <summary>
    /// Shuffles the server queue.
    /// </summary>
    /// <param name="guildId">Id of the server.</param>
    void ShuffleQueue(ulong guildId);
}