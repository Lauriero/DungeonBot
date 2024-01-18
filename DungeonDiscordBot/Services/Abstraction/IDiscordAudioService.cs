using DungeonDiscordBot.Model;

namespace DungeonDiscordBot.Services.Abstraction;

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
    Task PlayQueueAsync(ulong guildId, string reason = "", bool force = false);

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
    /// Removes the track from the queue.
    /// </summary>
    /// <param name="guildId">ID of the guild the target queue belongs to.</param>
    /// <param name="index">
    /// Index of the track that needs to be removed.
    /// Allowed values for this parameter are [0..n], that point out to the track on the [2..(n+2)] position in the queue,
    /// where n+2 is the number of tracks in the queue. 
    /// </param>
    Task RemoveTrackFromQueue(ulong guildId, int index);
    
    /// <summary>
    /// Removes the range of the tracks from the queue.
    /// </summary>
    /// <param name="guildId">ID of the guild the target queue belongs to.</param>
    /// <param name="range">
    /// Range of the indexes that point out to which tracks should be removed.
    /// Allowed values for this parameter are [0..n], that point out to the track on the [2..(n+2)] position in the queue.
    /// </param>
    Task RemoveTracksFromQueue(ulong guildId, Range range);

    /// <summary>
    /// Swaps the tracks on positions that correlate to the <paramref name="index1"/>
    /// and <paramref name="index2"/> parameters.
    /// </summary>
    /// <param name="guildId">ID of the guild the target queue belongs to.</param>
    /// <param name="index1">
    /// Index to the track that is to be swapped with the track with the <paramref name="index2"/>.
    /// </param>
    /// <param name="index2">
    /// Index to the track that is to be swapped with the track with the <paramref name="index1"/>.
    /// </param>
    /// <remarks>
    /// Allowed values for the <paramref name="index1"/> and <paramref name="index2"/> parameters
    /// are [0..n], that point out to the track on the [2..(n+2)] position in the queue.
    /// </remarks>
    Task SwapTracks(ulong guildId, int index1, int index2);

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