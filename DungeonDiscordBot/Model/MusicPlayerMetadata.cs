using System.Collections.Concurrent;

using Discord.Audio;
using Discord.WebSocket;

namespace DungeonDiscordBot.Model;

public class MusicPlayerMetadata
{
    public TaskCompletionSource? PlayerStoppedCompletionSource { get; set; }
    
    public CancellationTokenSource? PlayerCancellationTokenSource { get; set; }
    
    public IAudioClient? AudioClient { get; set; }

    public SocketVoiceChannel? VoiceChannel { get; set; }
    
    public int PageNumber { get; set; } = 1;

    public bool StopRequested { get; set; } = false;

    public bool ReconnectRequested { get; set; } = false;
    
    public TimeSpan Elapsed { get; set; } = TimeSpan.Zero;
    
    public MusicPlayerState State { get; set; } = MusicPlayerState.Stopped;

    public RepeatMode RepeatMode { get; set; } = RepeatMode.NoRepeat;

    public ConcurrentStack<AudioQueueRecord> PreviousTracks { get; } = new ConcurrentStack<AudioQueueRecord>();

    public ConcurrentQueue<AudioQueueRecord> Queue { get; } = new ConcurrentQueue<AudioQueueRecord>();

    public Timer? ElapsedTimer { get; set; }
}

public enum MusicPlayerState
{
    Stopped = 0,
    Paused = 1,
    Playing = 2,
}

public enum RepeatMode
{
    NoRepeat = 0,
    RepeatSong = 1,
    RepeatQueue = 2
}