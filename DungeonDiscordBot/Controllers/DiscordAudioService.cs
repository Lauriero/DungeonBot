using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;

using Discord;
using Discord.Audio;
using Discord.WebSocket;

using DungeonDiscordBot.Controllers.Abstraction;
using DungeonDiscordBot.Model;
using DungeonDiscordBot.Settings;
using DungeonDiscordBot.Utilities;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DungeonDiscordBot.Controllers;

public class DiscordAudioService : IDiscordAudioService
{
    private readonly ILogger<IDiscordAudioService> _logger;
    private readonly IUserInterfaceService _UIService;

    private readonly AppSettings _settings;
    private readonly ConcurrentDictionary<ulong, SocketVoiceChannel> _guildChannels = new();
    private readonly ConcurrentDictionary<ulong, IAudioClient> _connectedChannels = new();
    private readonly ConcurrentDictionary<ulong, ConcurrentQueue<AudioQueueRecord>> _guildQueues = new();
    private readonly ConcurrentDictionary<ulong, CancellationTokenSource> _guildCancellationTokens = new();
    private readonly ConcurrentDictionary<ulong, MusicPlayerMetadata> _guildMetadatas = new();

    public DiscordAudioService(ILogger<IDiscordAudioService> logger, IUserInterfaceService uiService, IOptions<AppSettings> settings)
    {
        _logger = logger;
        _UIService = uiService;
        _settings = settings.Value;
    }
    
    /// <inheritdoc /> 
    public void AddAudio(ulong guildId, AudioQueueRecord audio)
    {
        GetQueue(guildId).Enqueue(audio);
    }

    /// <inheritdoc /> 
    public void AddAudios(ulong guildId, IEnumerable<AudioQueueRecord> audios, bool addToHead = false)
    {
        var queue = GetQueue(guildId);
        if (!addToHead || queue.Count is 0 or 1) {
            foreach (AudioQueueRecord audio in audios) {
                queue.Enqueue(audio);
            }

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
    }

    /// <inheritdoc /> 
    public async Task PlayQueueAsync(ulong guildId, string reason = "")
    {
        MusicPlayerMetadata metadata = GetMusicPlayerMetadata(guildId);
        if (metadata.State == MusicPlayerState.Playing && !metadata.StopRequested) {
            await UpdateSongsQueueAsync(guildId, message: reason);
            return;
        }
        
        CancellationTokenSource cts = new CancellationTokenSource();
        _guildCancellationTokens.AddOrUpdate(guildId,
            _ => cts,
            (_, _) => cts); 
        
        IAudioClient client = await ConnectToChannelAsync(guildId);
        var queue = GetQueue(guildId);
        ThreadPool.QueueUserWorkItem
            <(ConcurrentQueue<AudioQueueRecord>, IAudioClient, ulong, string, CancellationToken)>
            (async (s) => 
                await PlayNextRecord(s.Item1, s.Item2, s.Item3, s.Item4, s.Item5), 
                (queue, client, guildId, reason, cts.Token),
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

        ConcurrentQueue<AudioQueueRecord> queue = GetQueue(guildId);
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
        await PausePlayerAsync(guildId);

        ConcurrentQueue<AudioQueueRecord> queue = GetQueue(guildId);
        if (!queue.TryDequeue(out AudioQueueRecord? record)) {
            throw new InvalidOperationException("Error dequeuing audio from the queue.");
        }

        MusicPlayerMetadata metadata = GetMusicPlayerMetadata(guildId);
        metadata.Elapsed = TimeSpan.Zero;
        metadata.PreviousTracks.Push(record);
        if (metadata.RepeatMode == RepeatMode.RepeatQueue) {
            queue.Enqueue(record);
        }
        
        await PlayQueueAsync(guildId);
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
            return;
        }
        
        metadata.PlayerStoppedCompletionSource = new TaskCompletionSource();
        metadata.StopRequested = true;
        
        if (_guildCancellationTokens.TryRemove(guildId, out CancellationTokenSource? cts)) {
            cts.Cancel();
            cts.Dispose();
        }

        await metadata.PlayerStoppedCompletionSource.Task;
        metadata.ElapsedTimer = null;
    }
    
    private async Task StopPlayerAsync(ulong guildId)
    {
        MusicPlayerMetadata metadata = GetMusicPlayerMetadata(guildId);
        if (metadata.State == MusicPlayerState.Stopped) {
            return;
        }

        // Stopping player gracefully, waiting for it to finish all processes
        if (metadata.State == MusicPlayerState.Playing) {
            metadata.PlayerStoppedCompletionSource = new TaskCompletionSource();
            metadata.StopRequested = true;
            
            if (_guildCancellationTokens.TryRemove(guildId, out CancellationTokenSource? cts)) {
                cts.Cancel();
                cts.Dispose();
            }

            await metadata.PlayerStoppedCompletionSource.Task;
        }
        
        // Reset metadatas and queue
        GetQueue(guildId).Clear();
        metadata.State = MusicPlayerState.Stopped;
        metadata.Elapsed = TimeSpan.Zero;
        metadata.ElapsedTimer = null;
        
        // Disconnect from a discord channel
        if (_guildChannels.TryGetValue(guildId, out SocketVoiceChannel? channel) &&
            _connectedChannels.TryRemove(channel.Id, out IAudioClient? client)) {
            
            await client.StopAsync();
            await channel.DisconnectAsync();
        }
        
        // Updating song queue message
        await UpdateSongsQueueAsync(guildId, "Queue has been cleared");
    }

    /// <inheritdoc /> 
    public void RegisterChannel(SocketGuild guild, ulong channelId)
    {
        if (!_guildQueues.ContainsKey(guild.Id)) {
            if (!_guildQueues.TryAdd(guild.Id, new ConcurrentQueue<AudioQueueRecord>())) {
                throw new InvalidOperationException("Unable to add queue for the server");
            }
        }
        
        _guildChannels.AddOrUpdate(guild.Id,
            (_) => guild.GetVoiceChannel(channelId),
            (_, _) => guild.GetVoiceChannel(channelId));
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
    public ConcurrentQueue<AudioQueueRecord> GetQueue(ulong guildId)
    {
        if (!_guildQueues.TryGetValue(guildId, out ConcurrentQueue<AudioQueueRecord>? queue)) {
            return new ConcurrentQueue<AudioQueueRecord>();
        }

        return queue;
    }

    /// <inheritdoc />
    public async Task ShuffleQueue(ulong guildId)
    {
        ConcurrentQueue<AudioQueueRecord> queue = GetQueue(guildId);
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
        if (!_guildChannels.TryGetValue(guildId, out SocketVoiceChannel? channel)) {
            throw new ArgumentException("No voice channels are registered for this server", nameof(guildId));
        }
            
        if (_connectedChannels.TryGetValue(channel.Id, out IAudioClient? value)) {
            if (value.ConnectionState == ConnectionState.Connected) {
                return value;
            } 
            
            if (!_connectedChannels.TryRemove(channel.Id, out IAudioClient? _)) {
                throw new Exception("Error removing audio client from the dictionary") ;
            }
        }
        
        IAudioClient client = await channel.ConnectAsync(selfDeaf: true);
        if (!_connectedChannels.TryAdd(channel.Id, client)) {
            _logger.Log(LogLevel.Error, "Error adding connected channel audios client to a collection");
        }

        return client;
    }

    private async Task PlayNextRecord(ConcurrentQueue<AudioQueueRecord> queue, IAudioClient client, 
        ulong guildId, string reason, CancellationToken token = default)
    {
        MusicPlayerMetadata metadata = GetMusicPlayerMetadata(guildId);
        metadata.State = MusicPlayerState.Playing;

        while (!queue.IsEmpty) {
            await UpdateSongsQueueAsync(guildId, token: token, message: reason);
            
            if (!queue.TryPeek(out AudioQueueRecord? record)) {
                throw new InvalidOperationException("Error peeking in the audio queue.");
            }

            TimeSpan elapsedBeforeStart = metadata.Elapsed;
            Stopwatch watch = new Stopwatch();
            watch.Start();

            if (metadata.ElapsedTimer is not null) {
                await metadata.ElapsedTimer.DisposeAsync();
            } 
            
            metadata.ElapsedTimer = new Timer(
                das => {
                    (DiscordAudioService, Stopwatch watch, TimeSpan) tuple = ((DiscordAudioService, Stopwatch, TimeSpan))das!;
                    metadata.Elapsed = tuple.Item2.Elapsed + tuple.Item3;
                    tuple.Item1.UpdateSongsQueueAsync(guildId, token: default)
                        .GetAwaiter().GetResult();
                }, 
                (this, watch, elapsedBeforeStart), 
                dueTime: TimeSpan.FromSeconds(record.Duration.TotalSeconds / _UIService.ProgressBarsCount), 
                TimeSpan.FromSeconds(record.Duration.TotalSeconds / _UIService.ProgressBarsCount)); // Bars count
        
            
            using (Process ffmpeg = CreateProcess(await record.AudioUrl.Value, metadata.Elapsed))
            await using (Stream output = ffmpeg.StandardOutput.BaseStream)
            await using (AudioOutStream? discord = client.CreatePCMStream(AudioApplication.Mixed)) {
                try {
                    await output.CopyToAsync(discord, token);
                } catch (OperationCanceledException) {
                } finally {
                    watch.Stop();
                    if (metadata.ElapsedTimer is not null) {
                        await metadata.ElapsedTimer.DisposeAsync();
                        metadata.ElapsedTimer = null;
                    }
                    
                    await discord.FlushAsync();
                    if (!ffmpeg.HasExited) {
                        ffmpeg.Kill(true);
                    }
                }
            }

            if (token.IsCancellationRequested) {
                if (metadata.StopRequested) {
                    metadata.State = MusicPlayerState.Paused;
                    metadata.StopRequested = false;
                    metadata.PlayerStoppedCompletionSource?.SetResult();
                    return;
                }

                metadata.Elapsed = watch.Elapsed + elapsedBeforeStart;
                return;
            }

            metadata.Elapsed = TimeSpan.Zero;
            if (metadata.RepeatMode != RepeatMode.RepeatSong) {
                metadata.PreviousTracks.Push(record);
                if (!queue.TryDequeue(out AudioQueueRecord? _)) {
                    throw new InvalidOperationException("Error dequeuing audio from the queue.");
                }
            }
            
            if (metadata.RepeatMode == RepeatMode.RepeatQueue) {
                queue.Enqueue(record);
            }
        }
        
        await ClearQueue(guildId);
    }

    public Task UpdateSongsQueueAsync(ulong guildId, string message = "", CancellationToken token = default)
    {
        MusicPlayerMetadata metadata = GetMusicPlayerMetadata(guildId);
        return _UIService.UpdateSongsQueueMessageAsync(guildId, GetQueue(guildId), 
            metadata, message, token);
    }
    
    private Process CreateProcess(string path, TimeSpan startTime)
    {
        Process? process = Process.Start(new ProcessStartInfo
        {
            FileName = _settings.FFMpegExecutable,
            Arguments = $"-hide_banner -err_detect ignore_err -ec guess_mvs+deblock+favor_inter -ignore_unknown " +
                        $"-reconnect 1 -reconnect_streamed 1 -reconnect_delay_max 5 " +
                        $"-re -i \"{path}\" -ac 2 -f s16le " +
                        $"-ss {startTime:hh\\:mm\\:ss} -ar 48000 pipe:1",
            UseShellExecute = false,
            RedirectStandardOutput = true
        });
        
        if (process is null) {
            throw new InvalidOperationException("Unable to start process");
        }

        return process;
    }
}