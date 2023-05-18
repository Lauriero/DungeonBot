using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Discord;
using Discord.Audio;
using Discord.WebSocket;

using DungeonDiscordBot.Controllers.Abstraction;
using DungeonDiscordBot.Model;
using DungeonDiscordBot.Utilities;

using Microsoft.Extensions.Logging;

namespace DungeonDiscordBot.Controllers;

public class DiscordAudioService : IDiscordAudioService
{
    private readonly ILogger<IDiscordAudioService> _logger;
    private readonly IUserInterfaceService _UIService;

    private readonly ConcurrentDictionary<ulong, SocketVoiceChannel> _guildChannels = new();
    private readonly ConcurrentDictionary<ulong, IAudioClient> _connectedChannels = new();
    private readonly ConcurrentDictionary<ulong, ConcurrentQueue<AudioQueueRecord>> _guildQueues = new();
    private readonly ConcurrentDictionary<ulong, CancellationTokenSource> _guildCancellationTokens = new();
    private readonly ConcurrentDictionary<ulong, MusicPlayerMetadata> _guildMetadatas = new();

    public DiscordAudioService(ILogger<IDiscordAudioService> logger, IUserInterfaceService uiService)
    {
        _logger = logger;
        _UIService = uiService;
    }
    
    /// <inheritdoc /> 
    public void AddAudio(ulong guildId, AudioQueueRecord audio)
    {
        GetQueue(guildId).Enqueue(audio);
    }
    
    /// <inheritdoc /> 
    public void AddAudios(ulong guildId, IEnumerable<AudioQueueRecord> audios)
    {
        var queue = GetQueue(guildId);
        foreach (AudioQueueRecord audio in audios) {
            queue.Enqueue(audio);
        }
    }

    /// <inheritdoc /> 
    public async Task PlayQueueAsync(ulong guildId)
    {
        var queue = GetQueue(guildId);
        
        CancellationTokenSource cts = new CancellationTokenSource();
        _guildCancellationTokens.AddOrUpdate(guildId,
            _ => cts,
            (_, _) => cts); 
        
        IAudioClient client = await ConnectToChannelAsync(guildId);
        ThreadPool.QueueUserWorkItem<
            (ConcurrentQueue<AudioQueueRecord>, IAudioClient, ulong, CancellationToken)>
            (async (s) => 
                await PlayNextRecord(s.Item1, s.Item2, s.Item3, s.Item4), 
                (queue, client, guildId, cts.Token),
                false);
    }

    /// <inheritdoc /> 
    public async Task PauseQueueAsync(ulong guildId)
    {
        if (!_guildCancellationTokens.TryRemove(guildId, out CancellationTokenSource? cts)) {
            return;
        }
        
        cts.Cancel();
        cts.Dispose();

        GetMusicPlayerMetadata(guildId).State = MusicPlayerState.Paused;
        await UpdateSongsQueueAsync(guildId, message: "Queue has been stopped");
    }

    public async Task PlayPreviousTrackAsync(ulong guildId)
    {
        if (!_guildQueues.TryGetValue(guildId, out ConcurrentQueue<AudioQueueRecord>? queue)) {
            throw new ArgumentException("No queue found for this guild");
        }
        
        MusicPlayerMetadata metadata = GetMusicPlayerMetadata(guildId);
        if (metadata.PreviousTracks.IsEmpty) {
            throw new ArgumentException("This guild has no previous tracks");
        }
        
        metadata.StopRequested = true;
        metadata.PlayPreviousRequested = true;
        metadata.Elapsed = TimeSpan.Zero;

        if (metadata.State == MusicPlayerState.Playing) {
            if (!_guildCancellationTokens.TryRemove(guildId, out CancellationTokenSource? cts)) {
                return;
            }
            
            cts.Cancel();
            cts.Dispose();
        }
        

        if (!metadata.PreviousTracks.TryPop(out AudioQueueRecord? previousRecord)) {
            throw new ArgumentException("This guild has no previous tracks");
        }

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
        if (!_guildCancellationTokens.TryRemove(guildId, out CancellationTokenSource? cts)) {
            return;
        }

        if (!_guildChannels.TryGetValue(guildId, out SocketVoiceChannel? channel)) {
            throw new ArgumentException("No voice channels are registered for this server", nameof(guildId));
        }

        MusicPlayerMetadata metadata = GetMusicPlayerMetadata(guildId);
        metadata.StopRequested = true;
        metadata.Elapsed = TimeSpan.Zero;
        
        cts.Cancel();
        cts.Dispose();
        
        if (!GetQueue(guildId).TryDequeue(out AudioQueueRecord? _)) {
            throw new InvalidOperationException("Error dequeuing audio from the queue.");
        }

        await PlayQueueAsync(guildId);
    }

    /// <inheritdoc /> 
    public async Task ClearQueue(ulong guildId)
    {
        if (!_guildCancellationTokens.TryGetValue(guildId, out CancellationTokenSource? cts)) {
            return;
        }
        
        if (!_guildChannels.TryGetValue(guildId, out SocketVoiceChannel? channel)) {
            return;
        }
            
        if (!_connectedChannels.TryRemove(channel.Id, out IAudioClient? client)) {
            return;
        }
        
        MusicPlayerMetadata metadata = GetMusicPlayerMetadata(guildId);
        metadata.StopRequested = true;
        metadata.Elapsed = TimeSpan.Zero;

        cts.Cancel();
        cts.Dispose();
        
        await client.StopAsync();
        await channel.DisconnectAsync();

        GetQueue(guildId).Clear();
        metadata.State = MusicPlayerState.Stopped;
        await UpdateSongsQueueAsync(guildId, message: "Queue has been cleared");
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

    private async Task PlayNextRecord(ConcurrentQueue<AudioQueueRecord> queue, IAudioClient client, ulong guildId, CancellationToken token = default)
    {
        MusicPlayerMetadata metadata = GetMusicPlayerMetadata(guildId);
        metadata.State = MusicPlayerState.Playing;

        while (!queue.IsEmpty) {
            await UpdateSongsQueueAsync(guildId, token: token);
            
            if (!queue.TryPeek(out AudioQueueRecord? record)) {
                throw new InvalidOperationException("Error peeking in the audio queue.");
            }

            Stopwatch watch = new Stopwatch();
            watch.Start();

            using (Process ffmpeg = CreateProcess(await record.AudioUrl.Value, metadata.Elapsed, token))
            await using (Stream output = ffmpeg.StandardOutput.BaseStream)
            await using (AudioOutStream? discord = client.CreatePCMStream(AudioApplication.Mixed)) {
                try {
                    await output.CopyToAsync(discord, token);
                } catch (OperationCanceledException) {
                } finally {
                    if (!token.IsCancellationRequested) {
                        await discord.FlushAsync(token);
                    }

                    watch.Stop();
                }
            }

            if (watch.IsRunning) {
                watch.Stop();
            }

            if (token.IsCancellationRequested) {
                if (metadata.StopRequested) {
                    metadata.StopRequested = false;

                    if (!metadata.PlayPreviousRequested) {
                        metadata.PreviousTracks.Push(record);
                    } else {
                        metadata.PlayPreviousRequested = false;
                    }

                    if (metadata.RepeatMode == RepeatMode.RepeatQueue) {
                        queue.Enqueue(record);
                    }

                    await UpdateSongsQueueAsync(guildId);
                    return;
                }

                if (metadata.Elapsed.TotalSeconds > 1) {
                    metadata.Elapsed = metadata.Elapsed.Add(watch.Elapsed);
                } else {
                    metadata.Elapsed = watch.Elapsed;
                }

                return;
            }

            metadata.Elapsed = TimeSpan.Zero;
            if (metadata.RepeatMode != RepeatMode.RepeatSong) {
                if (!queue.TryDequeue(out AudioQueueRecord? _)) {
                    throw new InvalidOperationException("Error dequeuing audio from the queue.");
                }
                
            }

            if (metadata.PlayPreviousRequested) {
                metadata.PlayPreviousRequested = false;
            }
            
            metadata.PreviousTracks.Push(record);
            if (metadata.RepeatMode == RepeatMode.RepeatQueue) {
                queue.Enqueue(record);
            }

            metadata.PlayPreviousRequested = false;
        }
        
        await ClearQueue(guildId);
    }

    public Task UpdateSongsQueueAsync(ulong guildId, int? pageNumber = null, string message = "", CancellationToken token = default)
    {
        MusicPlayerMetadata metadata = GetMusicPlayerMetadata(guildId);
        if (pageNumber is not null) {
            metadata.PageNumber = pageNumber.Value;
        }
        
        return _UIService.UpdateSongsQueueMessageAsync(guildId, GetQueue(guildId), 
            metadata, message, token);
    }
    
    private Process CreateProcess(string path, TimeSpan startTime, CancellationToken token = default)
    {
        Process? process = Process.Start(new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-hide_banner -i \"{path}\" -ac 2 -f s16le " +
                        $"-ss {startTime:hh\\:mm\\:ss} -ar 48000 -http_persistent false pipe:1",
            UseShellExecute = false,
            RedirectStandardOutput = true
        });
        
        if (process is null) {
            throw new InvalidOperationException("Unable to start process");
        }

        Task.Run(async () => {
            await process.WaitForExitAsync(token);
            if (!process.HasExited) {
                process.Kill();
                process.Dispose();
            }
        });
            
        return process;
    }
}