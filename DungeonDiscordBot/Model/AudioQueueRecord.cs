using DungeonDiscordBot.Model.MusicProviders;
using DungeonDiscordBot.Utilities;

namespace DungeonDiscordBot.Model;

[Serializable]
public class AudioQueueRecord
{
    /// <summary>
    /// Provider that has fetched this audio.
    /// </summary>
    public MusicProvider Provider { get; }
    
    /// <summary>
    /// Uri to the audio in the public resource.
    /// </summary>
    public string? PublicUrl { get; }

    public string Author { get; }
    
    public string Title { get; }
    
    public TimeSpan Duration { get; }

    /// <summary>
    /// Uri to the audio content.
    /// </summary>
    public AsyncLazy<string> AudioUrl { get; }
    
    public AsyncLazy<string?> AudioThumbnailUrl { get; }

    public AudioQueueRecord(MusicProvider provider, string author, string title,
        Func<Task<string>> audioUriFactory, 
        Func<Task<string?>> audioThumbnailUriFactory,
        TimeSpan duration, string? publicUrl)
    {
        Provider = provider;
        Author = author;
        Title = title;
        Duration = duration;
        PublicUrl = publicUrl;
        AudioUrl = new AsyncLazy<string>(audioUriFactory);
        AudioThumbnailUrl = new AsyncLazy<string?>(audioThumbnailUriFactory);
    }
    
    public AudioQueueRecord(MusicProvider provider, string author, string title,
        string audioUri, 
        string? audioThumbnailUri,
        TimeSpan duration, string? publicUrl)
    {
        Provider = provider;
        Author = author;
        Title = title;
        Duration = duration;
        PublicUrl = publicUrl;
        AudioUrl = new AsyncLazy<string>(() => audioUri);
        AudioThumbnailUrl = new AsyncLazy<string?>(() => audioThumbnailUri);
    }
}