using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Discord.Audio;
using Discord.WebSocket;

using DungeonDiscordBot.Controllers.Abstraction;
using DungeonDiscordBot.Model;
using DungeonDiscordBot.Utilities;

using Serilog;

namespace DungeonDiscordBot.Controllers;

public class DiscordAudioController : IDiscordAudioController
{
    private readonly ILogger _logger; 
    private IServicesAggregator _aggregator = null!;

    private readonly ConcurrentDictionary<ulong, SocketVoiceChannel> _guildChannels = new();
    private readonly ConcurrentDictionary<ulong, IAudioClient> _connectedChannels = new();
    private readonly ConcurrentDictionary<ulong, ConcurrentQueue<AudioQueueRecord>> _guildQueues = new();
    private readonly ConcurrentDictionary<ulong, CancellationTokenSource> _guildCancellationTokens = new();

    public DiscordAudioController(ILogger logger)
    {
        _logger = logger;
    }

    public async Task Init(IServicesAggregator aggregator)
    {
        _aggregator = aggregator;
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
            (_) => cts,
            (_, _) => cts);
        
        IAudioClient client = await ConnectToChannelAsync(guildId);
        await PlayNextRecord(queue!, client, guildId, cts.Token);
    }

    /// <inheritdoc /> 
    public async Task StopQueueAsync(ulong guildId)
    {
        if (!_guildCancellationTokens.TryGetValue(guildId, out CancellationTokenSource? cts)) {
            return;
        }
        
        if (!_guildChannels.TryGetValue(guildId, out SocketVoiceChannel? channel)) {
            throw new ArgumentException("No voice channels are registered for this server", nameof(guildId));
        }
            
        if (!_connectedChannels.TryGetValue(channel!.Id, out IAudioClient? client)) {
            return;
        }

        await client!.StopAsync();
        await channel.DisconnectAsync();
        
        cts!.Cancel();
        cts.Dispose();
    }

    /// <inheritdoc /> 
    public void ClearQueue(ulong guildId)
    {
        GetQueue(guildId).Clear();
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

    /// <inheritdoc /> 
    public ConcurrentQueue<AudioQueueRecord> GetQueue(ulong guildId)
    {
        if (!_guildQueues.TryGetValue(guildId, out ConcurrentQueue<AudioQueueRecord>? queue)) {
            return new ConcurrentQueue<AudioQueueRecord>();
        }

        return queue!;
    }

    /// <inheritdoc />
    public void ShuffleQueue(ulong guildId)
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
    }

    private async Task<IAudioClient> ConnectToChannelAsync(ulong guildId)
    {
        if (!_guildChannels.TryGetValue(guildId, out SocketVoiceChannel? channel)) {
            throw new ArgumentException("No voice channels are registered for this server", nameof(guildId));
        }
            
        if (_connectedChannels.TryGetValue(channel!.Id, out IAudioClient? value)) {
            return value!;
        }
        
        IAudioClient client = await channel.ConnectAsync(selfDeaf: true);
        if (!_connectedChannels.TryAdd(channel.Id, client)) {
            _logger.Error("Error adding connected channel audios client to a collection");
        }

        return client;
    }

    private async Task PlayNextRecord(ConcurrentQueue<AudioQueueRecord> queue, IAudioClient client, ulong guildId,
        CancellationToken token = default)
    {
        if (queue.IsEmpty || token.IsCancellationRequested) {
            await StopQueueAsync(guildId);
            return;
        }

        if (!queue.TryPeek(out AudioQueueRecord? record)) {
            throw new InvalidOperationException("Error peeking in the audio queue.");
        }

        using (Process ffmpeg = CreateStream(record.AudioUri.AbsoluteUri, token))
        await using (Stream output = ffmpeg.StandardOutput.BaseStream)
        await using (AudioOutStream? discord = client.CreatePCMStream(AudioApplication.Mixed))
        {
            try {
                await output.CopyToAsync(discord, token);
            } finally {
                await discord.FlushAsync(token);
            }
        }
        
        if (token.IsCancellationRequested) {
            await StopQueueAsync(guildId);
        }
        
        if (!queue.TryDequeue(out AudioQueueRecord? _)) {
            throw new InvalidOperationException("Error dequeuing audio from the queue.");
        }
        
        await PlayNextRecord(queue, client, guildId, token);
    }
    
    private Process CreateStream(string path, CancellationToken token = default)
    {
        Process? process = Process.Start(new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-http_persistent false -hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
            UseShellExecute = false,
            RedirectStandardOutput = true,
        });

        if (process is null) {
            throw new InvalidOperationException("Unable to start process");
        }
        
        Task.Run(async () => { 
            await token.WaitForCancellationAsync();
            if (!process.HasExited) {
                process.Kill();
            }
        });
            
        return process;
    }
}