namespace DungeonDiscordBot.Model;

public class MusicPlayerMetadata
{
    public int PageNumber { get; set; } = 1;

    public bool SkipRequested { get; set; } = false;

    public TimeSpan Elapsed { get; set; } = TimeSpan.Zero;
    
    public MusicPlayerState State { get; set; } = MusicPlayerState.Stopped;

    public RepeatMode RepeatMode { get; set; } = RepeatMode.NoRepeat;
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