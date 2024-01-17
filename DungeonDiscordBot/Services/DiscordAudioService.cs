using System.Collections.Concurrent;
using System.Diagnostics;

using Discord;
using Discord.Audio;
using Discord.Net;

using DungeonDiscordBot.Model;
using DungeonDiscordBot.Services.Abstraction;
using DungeonDiscordBot.Settings;
using DungeonDiscordBot.Utilities;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DungeonDiscordBot.Services;

public class DiscordAudioService : IDiscordAudioService
{
    private readonly ILogger<IDiscordAudioService> _logger;
    private readonly IDataStorageService _dataStorage;
    private readonly IUserInterfaceService _UIService;

    private readonly AppSettings _settings;
    private readonly ConcurrentDictionary<ulong, MusicPlayerMetadata> _guildMetadatas = new();

    public DiscordAudioService(ILogger<IDiscordAudioService> logger, IUserInterfaceService uiService, 
        IOptions<AppSettings> settings, IDataStorageService dataStorage)
    {
        _logger = logger;
        _UIService = uiService;
        _dataStorage = dataStorage;
        _settings = settings.Value;
    }
    
    /// <inheritdoc /> 
    public void AddAudio(ulong guildId, AudioQueueRecord audio)
    {
        GetMusicPlayerMetadata(guildId).Queue.Enqueue(audio);
    }

    /// <inheritdoc /> 
    public async Task AddAudios(ulong guildId, IEnumerable<AudioQueueRecord> audios, bool addToHead = false)
    {
        ConcurrentQueue<AudioQueueRecord> queue = GetMusicPlayerMetadata(guildId).Queue;
        if (!addToHead || queue.Count is 0 or 1) {
            foreach (AudioQueueRecord audio in audios) {
                queue.Enqueue(audio);
            }

            await UpdateSongsQueueAsync(guildId);
            return;
        }

        List<AudioQueueRecord> queueContents = new List<AudioQueueRecord>(queue);

        queue.Clear();
        queue.Enqueue(queueContents.First());
        foreach (AudioQueueRecord audio in audios) {
            queue.Enqueue(audio);
        }
        
        for (int i = 1; i < queueContents.Count; i++) {
            queue.Enqueue(queueContents[i]);
        }
        
        await UpdateSongsQueueAsync(guildId);
    }

    /// <inheritdoc /> 
    public async Task PlayQueueAsync(ulong guildId, string reason = "", bool force = false)
    {
        MusicPlayerMetadata metadata = GetMusicPlayerMetadata(guildId);
        if (metadata.State == MusicPlayerState.Playing && !metadata.StopRequested && !force) {
            await UpdateSongsQueueAsync(guildId, message: reason);
            return;
        }
        
        metadata.PlayerCancellationTokenSource = new CancellationTokenSource();

        IAudioClient client = await ConnectToChannelAsync(guildId);
        ThreadPool.QueueUserWorkItem
            <(ulong, string)>
            (async (s) => 
                await PlayNextRecord(s.Item1, s.Item2), 
                (guildId, reason),
                false);
    }

    /// <inheritdoc /> 
    public async Task PauseQueueAsync(ulong guildId)
    {
        await PausePlayerAsync(guildId);
        await UpdateSongsQueueAsync(guildId, message: "Queue has been stopped");
    }

    public async Task PlayPreviousTrackAsync(ulong guildId)
    {
        MusicPlayerMetadata metadata = GetMusicPlayerMetadata(guildId);
        if (!metadata.PreviousTracks.TryPop(out AudioQueueRecord? previousRecord)) {
            return;
        }

        await PausePlayerAsync(guildId);
        metadata.Elapsed = TimeSpan.Zero;

        ConcurrentQueue<AudioQueueRecord> queue = metadata.Queue;
        List<AudioQueueRecord> newQueue = new List<AudioQueueRecord>();
        newQueue.Add(previousRecord);
        newQueue.AddRange(queue);
        
        queue.Clear();
        foreach (AudioQueueRecord record in newQueue) {
            queue.Enqueue(record);
        }
        
        await PlayQueueAsync(guildId);
    }
    
    public async Task SkipTrackAsync(ulong guildId)
    {
        MusicPlayerMetadata metadata = GetMusicPlayerMetadata(guildId);
        ConcurrentQueue<AudioQueueRecord> queue = metadata.Queue;
        AudioQueueRecord? record = null;
        if (!queue.IsEmpty) {
            if (!queue.TryDequeue(out record)) {
                throw new InvalidOperationException("Error dequeuing audio from the queue.");
            }
        }
        
        await PausePlayerAsync(guildId);

        metadata.Elapsed = TimeSpan.Zero;
        if (record is not null) {
            metadata.PreviousTracks.Push(record);
            if (metadata.RepeatMode == RepeatMode.RepeatQueue) {
                queue.Enqueue(record);
            }
        }
        
        await PlayQueueAsync(guildId);
    }

    public Task RemoveTrackFromQueue(ulong guildId, int index)
    {
        throw new NotImplementedException();
    }

    public Task RemoveTracksFromQueue(ulong guildId, Range range)
    {
        throw new NotImplementedException();
    }

    public Task SwapTracks(ulong guildId, int index1, int index2)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc /> 
    public async Task ClearQueue(ulong guildId)
    {
        await StopPlayerAsync(guildId);
    }

    private async Task PausePlayerAsync(ulong guildId)
    {
        MusicPlayerMetadata metadata = GetMusicPlayerMetadata(guildId);
        if (metadata.State is MusicPlayerState.Stopped or MusicPlayerState.Paused || metadata.StopRequested) {
            _logger.LogInformation("Requested to stop music player that is already stopped in guild {id}", guildId);
            return;
        }
        
        metadata.PlayerStoppedCompletionSource = new TaskCompletionSource();
        metadata.StopRequested = true;
        
        CancellationTokenSource? cts = metadata.PlayerCancellationTokenSource;
        if (cts is not null) {
            cts.Cancel();
            cts.Dispose();
            metadata.PlayerCancellationTokenSource = null;
        }

        if (await Task.WhenAny(metadata.PlayerStoppedCompletionSource.Task, Task.Delay(5000)) 
            == metadata.PlayerStoppedCompletionSource.Task) {
            metadata.ElapsedTimer = null;
        } else {
            _logger.LogInformation("Timeout to stop the player exceeded in guild {id}", guildId);
            
            metadata.State = MusicPlayerState.Paused;
            await ClearQueue(guildId);
        }
    }
    
    private async Task StopPlayerAsync(ulong guildId)
    {
        MusicPlayerMetadata metadata = GetMusicPlayerMetadata(guildId);
        if (metadata.State == MusicPlayerState.Stopped) {
            return;
        }

        // Stopping player gracefully, waiting for it to finish all processes
        if (metadata.State == MusicPlayerState.Playing) {
            await PausePlayerAsync(guildId);
        }

        if (metadata.State == MusicPlayerState.Stopped) {
            return;
        }
        
        // Reset metadatas and queue
        metadata.Queue.Clear();
        metadata.State = MusicPlayerState.Stopped;
        metadata.Elapsed = TimeSpan.Zero;
        metadata.ElapsedTimer = null;
        
        // Disconnect from a discord channel
        if (metadata.VoiceChannel is not null && metadata.AudioClient is not null) {
            await metadata.AudioClient.StopAsync();
            await metadata.VoiceChannel.DisconnectAsync();
        }

        // Updating song queue message
        await UpdateSongsQueueAsync(guildId, "Queue has been cleared");
    }

    public MusicPlayerMetadata CreateMusicPlayerMetadata(ulong guildId)
    {
        MusicPlayerMetadata metadata = new MusicPlayerMetadata();
        _guildMetadatas.AddOrUpdate(guildId,
            _ => metadata,
            (_, _) => metadata);

        return metadata;
    }

    public MusicPlayerMetadata GetMusicPlayerMetadata(ulong guildId)
    {
        if (!_guildMetadatas.TryGetValue(guildId, out MusicPlayerMetadata? queue)) {
            throw new ArgumentException("No metadata registered for this guild",
                nameof(guildId));
        }

        return queue; 
    }

    /// <inheritdoc />
    public async Task ShuffleQueue(ulong guildId)
    {
        ConcurrentQueue<AudioQueueRecord> queue = GetMusicPlayerMetadata(guildId).Queue;
        List<AudioQueueRecord> toShuffle = queue.Skip(1).ToList();
        toShuffle.Shuffle();
     
        if (!queue.TryPeek(out AudioQueueRecord? firstRecord)) {
            throw new InvalidOperationException("Error peeking in the audio queue.");
        }
        
        queue.Clear();
        queue.Enqueue(firstRecord);

        foreach (AudioQueueRecord record in toShuffle) {
            queue.Enqueue(record);
        }

        await UpdateSongsQueueAsync(guildId);
    }

    private async Task<IAudioClient> ConnectToChannelAsync(ulong guildId)
    {
        MusicPlayerMetadata metadata = GetMusicPlayerMetadata(guildId);
        if (metadata.VoiceChannel is null) {
            throw new Exception("Voice channel was not registered before the connection");
        }

        if (metadata.AudioClient is not null) {
            if (metadata.AudioClient.ConnectionState == ConnectionState.Connected) {
                return metadata.AudioClient;
            }
        }
        
        IAudioClient client = await metadata.VoiceChannel.ConnectAsync(selfDeaf: true);
        client.Disconnected += async _ => {
            if (metadata.ReconnectRequested) {
                metadata.ReconnectRequested = false;
                if (metadata.State != MusicPlayerState.Stopped) {
                    await Task.Delay(1000);
                    await PlayQueueAsync(guildId, force: true);
                    client.Dispose();
                }
            }
        };
        
        metadata.AudioClient = client;
        return client;
    }

    private async Task PlayNextRecord(ulong guildId, string reason)
    {
        MusicPlayerMetadata metadata = GetMusicPlayerMetadata(guildId);
        CancellationToken token = metadata.PlayerCancellationTokenSource?.Token ?? default;
        
        while (!metadata.Queue.IsEmpty) {
            metadata.State = MusicPlayerState.Playing;
            await UpdateSongsQueueAsync(guildId, token: token, message: reason);
            if (!metadata.Queue.TryPeek(out AudioQueueRecord? record)) {
                throw new InvalidOperationException("Error peeking in the audio queue.");
            }

            try {
                if (record.AudioUrl is null) {
                    _logger.LogInformation("Fetching the audio url of track {artist} - {title} placed on {url}",
                        record.Author, record.Title, record.PublicUrl);
                    await record.UpdateAudioUrlAsync();
                } else {
                    if (!await HttpExtensions.RemoteFileExists(record.AudioUrl, TimeSpan.FromSeconds(3))) {
                        _logger.LogInformation("Updating the audio url due to the url of track {artist} - {title} " +
                                               "placed on {url} was not available",
                            record.Author, record.Title, record.PublicUrl);
                        await record.UpdateAudioUrlAsync();
                    }
                }

                if (!await HttpExtensions.RemoteFileExists(record.AudioUrl, TimeSpan.FromSeconds(3))) {
                    _logger.LogInformation("Skipping the track {artist} - {title} located at {url} " +
                                           "cause audio url is unavailable",
                        record.Author, record.Title, record.PublicUrl);

                    metadata.State = MusicPlayerState.Paused;
                    if (!metadata.Queue.TryDequeue(out AudioQueueRecord? _)) {
                        throw new InvalidOperationException("Error dequeuing audio from the queue.");
                    }

                    continue;
                }

                TimeSpan elapsedBeforeStart = metadata.Elapsed;
                Stopwatch watch = new Stopwatch();

                if (metadata.ElapsedTimer is not null) {
                    await metadata.ElapsedTimer.DisposeAsync();
                }

                metadata.ElapsedTimer = new Timer(das => {
                        (DiscordAudioService, Stopwatch watch, TimeSpan) tuple =
                            ((DiscordAudioService, Stopwatch, TimeSpan)) das!;
                        metadata.Elapsed = tuple.Item2.Elapsed + tuple.Item3;
                        tuple.Item1.UpdateSongsQueueAsync(guildId, token: default)
                            .GetAwaiter().GetResult();
                    },
                    (this, watch, elapsedBeforeStart),
                    dueTime: TimeSpan.FromSeconds(record.Duration.TotalSeconds / _UIService.ProgressBarsCount),
                    TimeSpan.FromSeconds(record.Duration.TotalSeconds / _UIService.ProgressBarsCount)); // Bars count

                watch.Start();

                if (metadata.Elapsed > record.Duration) {
                    if (metadata.RepeatMode == RepeatMode.NoRepeat) {
                        metadata.PreviousTracks.Push(record);
                        if (!metadata.Queue.TryDequeue(out AudioQueueRecord? _)) {
                            throw new InvalidOperationException("Error dequeuing audio from the queue.");
                        }
                    }
                }

                if (metadata.Elapsed < record.Duration) {
                    using (Process ffmpeg = CreateProcess(record.AudioUrl, metadata.Elapsed))
                    await using (Stream output = ffmpeg.StandardOutput.BaseStream)
                    await using (AudioOutStream? discord = metadata.AudioClient!.CreatePCMStream(AudioApplication.Mixed)) {
                        bool internalStopFlag = false;
                        try {
                            await output.CopyToAsync(discord, token);
                        } catch (OperationCanceledException) {
                        } finally {
                            watch.Stop();
                            if (metadata.ElapsedTimer is not null) {
                                await metadata.ElapsedTimer.DisposeAsync();
                                metadata.ElapsedTimer = null;
                            }

                            if (metadata.AudioClient.ConnectionState == ConnectionState.Connected) {
                                await discord.FlushAsync();
                            } else {
                                metadata.Elapsed = elapsedBeforeStart + watch.Elapsed;
                                internalStopFlag = true;
                            }
                            
                            if (!ffmpeg.HasExited) {
                                ffmpeg.Kill(true);
                            }
                        }

                        if (internalStopFlag) {
                            return;
                        }
                    }
                }

                if (token.IsCancellationRequested) {
                    if (metadata.StopRequested) {
                        metadata.State = MusicPlayerState.Paused;
                        metadata.StopRequested = false;

                        if (metadata.PlayerStoppedCompletionSource is not null && !metadata.PlayerStoppedCompletionSource.Task.IsCompleted) {
                            metadata.PlayerStoppedCompletionSource?.SetResult();
                        }

                        return;
                    }

                    metadata.Elapsed = watch.Elapsed + elapsedBeforeStart;

                    return;
                }

                metadata.State = MusicPlayerState.Paused;
                metadata.Elapsed = TimeSpan.Zero;

                if (metadata.RepeatMode != RepeatMode.RepeatSong) {
                    metadata.PreviousTracks.Push(record);
                    if (!metadata.Queue.TryDequeue(out AudioQueueRecord? _)) {
                        throw new InvalidOperationException("Error dequeuing audio from the queue.");
                    }
                }

                if (metadata.RepeatMode == RepeatMode.RepeatQueue) {
                    metadata.Queue.Enqueue(record);
                }
            } catch (Exception e) {
                _logger.LogInformation("Skipping the track due to the error occurred while playing the track {artist} - {title} " +
                                               "placed on {url}",
                    record.Author, record.Title, record.PublicUrl);
                _logger.LogDebug(e, "Error while playing the track: ");
                
                metadata.State = MusicPlayerState.Paused;
                metadata.PlayerStoppedCompletionSource?.SetResult();
                if (!metadata.Queue.TryDequeue(out AudioQueueRecord? _)) {
                    throw new InvalidOperationException("Error dequeuing audio from the queue.");
                }
            }
        }

        await ClearQueue(guildId);
    }

    public Task UpdateSongsQueueAsync(ulong guildId, string message = "", CancellationToken token = default)
    {
        MusicPlayerMetadata metadata = GetMusicPlayerMetadata(guildId);
        return _UIService.UpdateSongsQueueMessageAsync(guildId, metadata, message, token);
    }
    
    private Process CreateProcess(string path, TimeSpan startTime)
    {
        Process? process = Process.Start(new ProcessStartInfo
        {
            FileName = _settings.FFMpegExecutable,
            Arguments = $"-hide_banner -err_detect ignore_err -ignore_unknown " +
                        $"-reconnect 1 -reconnect_streamed 0 -reconnect_delay_max 5 " +
                        $"-re -ss {startTime:hh\\:mm\\:ss} -i \"{path}\" -ac 2 -f s16le " +
                        $"-ar 48000 pipe:1",
            UseShellExecute = false,
            RedirectStandardOutput = true
        });
        
        if (process is null) {
            throw new InvalidOperationException("Unable to start process");
        }

        return process;
    }
}