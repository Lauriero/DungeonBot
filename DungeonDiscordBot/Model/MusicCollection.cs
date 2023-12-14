namespace DungeonDiscordBot.Model;

/// <summary>
/// Contains music list with some metadata about the query.
/// If the audio collection has no entries, it is advised to set <see cref="Name"/> to "Not found".
/// </summary>
public class MusicCollection
{
    /// <summary>
    /// Provider that fetched the music for this collection.
    /// </summary>
    public MusicProvider Provider { get; }
    
    /// <summary>
    /// General name of the collection.
    /// May be the song full name (artist - song),
    /// album name (artist - album),
    /// playlist name, etc... 
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Array of audios that were fetched.
    /// </summary>
    public IEnumerable<AudioQueueRecord> Audios { get; }

    public MusicCollection(MusicProvider provider, string name, IEnumerable<AudioQueueRecord> audios)
    {
        Provider = provider;
        Name = name;
        Audios = audios;
    }
}