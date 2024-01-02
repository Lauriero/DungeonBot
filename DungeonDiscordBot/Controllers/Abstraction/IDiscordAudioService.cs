using System.Collections.Concurrent;

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
    /// <param name="addToHead">Flat that indicates whether audios should be put in the head or the the tail of the queue</param>
    Task AddAudios(ulong guildId, IEnumerable<AudioQueueRecord> audios, bool addToHead);

    /// <summary>
    /// Starts playing the queue.
    /// </summary>
    /// <param name="guildId">Id of the server.</param>
    Task PlayQueueAsync(ulong guildId, string reason = "");

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
    Task UpdateSongsQueueAsync(ulong guildId, string message = "", CancellationToken token = default);

    /// <summary>
    /// Shuffles the server queue.
    /// </summary>
    /// <param name="guildId">Id of the server.</param>
    Task ShuffleQueue(ulong guildId);
}