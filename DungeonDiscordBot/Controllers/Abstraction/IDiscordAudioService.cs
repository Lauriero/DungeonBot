using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using ConcurrentLinkedList;

using Discord.WebSocket;

using DungeonDiscordBot.Model;

namespace DungeonDiscordBot.Controllers.Abstraction;

public interface IDiscordAudioService
{
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
    Task PauseQueueAsync(ulong guildId);

    /// <summary>
    /// Puts track that has been playing the last time to the head of the queue.
    /// </summary>
    /// <param name="guildId"></param>
    /// <returns></returns>
    Task PlayPreviousTrackAsync(ulong guildId);
    
    /// <summary>
    /// Removes track that is currently playing from the queue.
    /// </summary>
    /// <param name="guildId"></param>
    /// <returns></returns>
    Task SkipTrackAsync(ulong guildId);

    /// <summary>
    /// Remove all songs from the queue.
    /// </summary>
    /// <param name="guildId">Id of the server.</param>
    Task ClearQueue(ulong guildId);

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
    /// Registers a metadata for the server.
    /// </summary>
    /// <returns></returns>
    MusicPlayerMetadata CreateMusicPlayerMetadata(ulong guildId);

    /// <summary>
    /// Gets metadata of the music player for the server.
    /// </summary>
    MusicPlayerMetadata GetMusicPlayerMetadata(ulong guildId);

    
    /// <summary>
    /// Updates queue message for this server.
    /// </summary>
    Task UpdateSongsQueueAsync(ulong guildId, int? pageNumber = null, string message = "", CancellationToken token = default);

    /// <summary>
    /// Shuffles the server queue.
    /// </summary>
    /// <param name="guildId">Id of the server.</param>
    Task ShuffleQueue(ulong guildId);
}