using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

using Discord.Audio;
using Discord.Interactions;
using Discord.WebSocket;

using DungeonDiscordBot.Controllers.Abstraction;
using DungeonDiscordBot.Model;
using DungeonDiscordBot.MusicProvidersControllers;
using DungeonDiscordBot.Utilities;


using Serilog;

namespace DungeonDiscordBot.InteractionModules;

public class MusicModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ConcurrentDictionary<ulong, IAudioClient> _connectedChannels = new();
    
    private readonly ILogger _logger;
    private readonly IServicesAggregator _aggregator;
    
    public MusicModule(IServicesAggregator aggregator, ILogger logger)
    {
        _logger = logger;
        _aggregator = aggregator;
    }

    [SlashCommand(
        name:        "play",
        description: "Plays song or playlist from the link or search request", 
        runMode:     RunMode.Async)]
    public async Task PlayAsync(
        [Summary("query", "Link to a song, playlist, video or a search query")]
        string query,
        
        [Summary("provider", "Name of the music providerController the search will be performed with (VK default)")]
        MusicProvider? provider = null
    )
    {
        await OnExceptionWrapper(async () => {
            provider ??= MusicProvider.VK;
            await DeferAsync();
            
            if (Uri.TryCreate(query, UriKind.Absolute, out Uri? link)
                && (link.Scheme == Uri.UriSchemeHttp || link.Scheme == Uri.UriSchemeHttps)) {

                BaseMusicProviderController? controller = link.FindMusicProviderController();
                if (controller is null) {
                    await RespondAsync("***This link is not supported***");
                    return;
                }

                SocketVoiceChannel? targetChannel = GetVoiceChannelWithCurrentUser();
                if (targetChannel is null) {
                    await RespondAsync("User is not found in any of the voice channels");
                    return;
                }
                
                controller.AudiosProcessingStarted += async (audiosCount) => {
                    await ModifyOriginalResponseAsync(m => m.Content = $"Processing 0/{audiosCount}");
                };

                controller.AudiosProcessingProgressed += async (audiosProcessed, audiosCount) => {
                    await ModifyOriginalResponseAsync(m => m.Content = $"Processing {audiosProcessed}/{audiosCount}");
                };

                controller.AudiosProcessed += async (audiosProcessed) => {
                    await ModifyOriginalResponseAsync(m => m.Content = $"{audiosProcessed} audios were processed");
                };
                
                IEnumerable<AudioQueueRecord> records = await controller.GetAudiosFromLink(link);

                _aggregator.DiscordAudio.RegisterChannel(Context.Guild, targetChannel.Id);
                _aggregator.DiscordAudio.AddAudios(Context.Guild.Id, records);
                
                await ModifyOriginalResponseAsync((m) => m.Content = $"Queue started");
                await _aggregator.DiscordAudio.PlayQueueAsync(Context.Guild.Id);
            } else {
                await RespondAsync("Searching...");
            }
        });
    }

    [SlashCommand("stop", "Stops playing the songs queue", runMode: RunMode.Async)]
    public async Task StopAsync()
    {
        await OnExceptionWrapper(async () => {
            throw new ArgumentException("Test");
            
            await _aggregator.DiscordAudio.StopQueueAsync(Context.Guild.Id);
            await RespondAsync("Stopped");
        });
    }

    [SlashCommand("queue", "Shows the list of songs that are currently playing", runMode: RunMode.Async)]
    public async Task ShowQueueAsync()
    {
        await OnExceptionWrapper(async () => {
            await DeferAsync();
            
            ConcurrentQueue<AudioQueueRecord> queue = _aggregator.DiscordAudio.GetQueue(Context.Guild.Id);
            await ModifyOriginalResponseAsync(m => m.GenerateQueueMessage(queue));
        });
    }

    [SlashCommand("shuffle", "Shuffles the list of songs", runMode: RunMode.Async)]
    public async Task ShuffleQueueAsync()
    {
        await OnExceptionWrapper(async () => {
            await DeferAsync();
            _aggregator.DiscordAudio.ShuffleQueue(Context.Guild.Id);
            await ModifyOriginalResponseAsync(m => m.Content = "Queue is shuffled");
        });
    }

    private async Task OnExceptionWrapper(Func<Task> inner)
    {
        try {
            await inner();
        } catch (Exception e) {
            await _aggregator.DiscordBot.HandleInteractionException(e);
        }
    }

    private SocketVoiceChannel? GetVoiceChannelWithCurrentUser()
    {
        foreach (SocketVoiceChannel? channel in Context.Guild.VoiceChannels) {
            foreach (SocketGuildUser user in channel.ConnectedUsers) {
                if (user.Id == Context.User.Id) {
                    return channel;
                }
            }
        }

        return null;
    }
}