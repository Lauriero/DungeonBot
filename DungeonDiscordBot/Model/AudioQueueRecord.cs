using System;

namespace DungeonDiscordBot.Model;

public class AudioQueueRecord
{
    public string Author { get; }
    
    public string Title { get; }
    
    public Uri AudioUri { get; }
    
    public string? AudioThumbnailUrl { get; }

    public AudioQueueRecord(string author, string title, Uri audioUri, string? audioThumbnailUrl)
    {
        Author = author;
        Title = title;
        AudioUri = audioUri;
        AudioThumbnailUrl = audioThumbnailUrl;
    }
}