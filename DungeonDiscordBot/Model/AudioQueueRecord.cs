using System.Diagnostics;
using System.Diagnostics.Contracts;

using DungeonDiscordBot.Utilities;

namespace DungeonDiscordBot.Model;

public class AudioQueueRecord
{
    public string Author { get; }
    
    public string Title { get; }
    
    public TimeSpan Duration { get; }

    public AsyncLazy<string> AudioUrl { get; }
    
    public AsyncLazy<string?> AudioThumbnailUrl { get; }

    public AudioQueueRecord(string author, string title,
        Func<Task<string>> audioUriFactory, 
        Func<Task<string?>> audioThumbnailUriFactory,
        TimeSpan duration)
    {
        Author = author;
        Title = title;
        Duration = duration;
        AudioUrl = new AsyncLazy<string>(audioUriFactory);
        AudioThumbnailUrl = new AsyncLazy<string?>(audioThumbnailUriFactory);
    }
}