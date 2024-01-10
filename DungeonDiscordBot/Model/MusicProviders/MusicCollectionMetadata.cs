namespace DungeonDiscordBot.Model.MusicProviders;

/// <summary>
/// Contains data about the collection of tracks.
/// </summary>
public class MusicCollectionMetadata
{
    /// <summary>
    /// Visible collection name.
    /// May be the song full name (artist - song),
    /// album name (artist - album),
    /// playlist name, etc...
    /// </summary>
    public string Name { get; set; } = null!;
    
    /// <summary>
    /// Url the collection is located at.
    /// </summary>
    public string PublicUrl { get; set; } = null!;

    /// <summary>
    /// Type of the collection.
    /// </summary>
    public MusicCollectionType Type { get; set; } = default!;
}