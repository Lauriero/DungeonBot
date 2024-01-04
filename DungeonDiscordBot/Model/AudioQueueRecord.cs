using System.Diagnostics.CodeAnalysis;

using DungeonDiscordBot.Model.MusicProviders;
using DungeonDiscordBot.Utilities;

namespace DungeonDiscordBot.Model;

public abstract class AudioQueueRecord
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
    /// Contains the last modified value of the url to the audio track.
    /// Or null if the value has never been modified and needs to be updated.
    /// </summary>
    public string? AudioUrl { get; protected set; }
    
    public AsyncLazy<string?> AudioThumbnailUrl { get; }
    
    protected AudioQueueRecord(MusicProvider provider, string author, string title,
        Func<Task<string?>> audioThumbnailUriFactory, TimeSpan duration, string? publicUrl)
    {
        Provider = provider;
        Author = author;
        Title = title;
        Duration = duration;
        AudioThumbnailUrl = new AsyncLazy<string?>(audioThumbnailUriFactory);
        PublicUrl = publicUrl;
    }
    
    /// <summary>
    /// Updates the value of the <see cref="AudioUrl"/> property.
    /// </summary>
    [MemberNotNull(nameof(AudioUrl))]
    public abstract Task UpdateAudioUrlAsync();
}