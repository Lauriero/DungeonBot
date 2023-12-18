using System.Diagnostics;
using System.Diagnostics.Contracts;

using DungeonDiscordBot.Model.MusicProviders;
using DungeonDiscordBot.Utilities;

namespace DungeonDiscordBot.Model;

public class AudioQueueRecord
{
    /// <summary>
    /// Provider that fetches this audio.
    /// </summary>
    public MusicProvider Provider { get; set; }

    public string Author { get; }
    
    public string Title { get; }
    
    public TimeSpan Duration { get; }

    public AsyncLazy<string> AudioUrl { get; }
    
    public AsyncLazy<string?> AudioThumbnailUrl { get; }

    public AudioQueueRecord(MusicProvider provider, string author, string title,
        Func<Task<string>> audioUriFactory, 
        Func<Task<string?>> audioThumbnailUriFactory,
        TimeSpan duration)
    {
        Provider = provider;
        Author = author;
        Title = title;
        Duration = duration;
        AudioUrl = new AsyncLazy<string>(audioUriFactory);
        AudioThumbnailUrl = new AsyncLazy<string?>(audioThumbnailUriFactory);
    }
    
    public AudioQueueRecord(MusicProvider provider, string author, string title,
        string audioUri, 
        string? audioThumbnailUri,
        TimeSpan duration)
    {
        Provider = provider;
        Author = author;
        Title = title;
        Duration = duration;
        AudioUrl = new AsyncLazy<string>(() => audioUri);
        AudioThumbnailUrl = new AsyncLazy<string?>(() => audioThumbnailUri);
    }
}