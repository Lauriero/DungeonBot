using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;

using DungeonDiscordBot.Utilities;

namespace DungeonDiscordBot.Model;

public class AudioQueueRecord
{
    public string Author { get; }
    
    public string Title { get; }

    public AsyncLazy<string> AudioUrl { get; }
    
    public AsyncLazy<string?> AudioThumbnailUrl { get; }

    public AudioQueueRecord(string author, string title,
        Func<Task<string>> audioUriFactory, 
        Func<Task<string?>> audioThumbnailUriFactory,
        TimeSpan duration = default)
    {
        Author = author;
        Title = title;
        AudioUrl = new AsyncLazy<string>(audioUriFactory);
        AudioThumbnailUrl = new AsyncLazy<string?>(audioThumbnailUriFactory);
        
        _duration = duration;
    }


    private TimeSpan _duration;

    public Task<TimeSpan> Duration
    {
        get {
            if (_duration != TimeSpan.Zero) {
                return Task.FromResult(_duration);
            }

            return Task.Run(async () => {
                _duration = TimeSpan.FromSeconds(await GetAudioFileDuration(await AudioUrl.Value));
                return _duration;
            });
        }
    }
    
    [Pure]
    private static async Task<double> GetAudioFileDuration(string source)
    {
        Process? process = Process.Start(new ProcessStartInfo {
            FileName = "ffprobe",
            Arguments = $"-i \"{source}\" -show_entries format=duration -v quiet -of csv=\"p=0\"",
            UseShellExecute = false,
            RedirectStandardOutput = true
        });
        
        if (process is null) {
            throw new InvalidOperationException("Unable to start process");
        }

        string? result = await process.StandardOutput.ReadLineAsync();
        if (result is null) {
            throw new InvalidOperationException("Process returned null");
        }

        return Convert.ToDouble(result);
    }
}