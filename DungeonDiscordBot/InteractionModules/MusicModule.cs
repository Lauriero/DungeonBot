using System.Collections.Concurrent;

using Discord.Audio;
using Discord.Interactions;
using Discord.WebSocket;

using DungeonDiscordBot.Controllers;
using DungeonDiscordBot.Controllers.Abstraction;
using DungeonDiscordBot.Exceptions;
using DungeonDiscordBot.Model;
using DungeonDiscordBot.Model.Database;
using DungeonDiscordBot.MusicProvidersControllers;
using DungeonDiscordBot.Utilities;

using Microsoft.Extensions.Logging;

namespace DungeonDiscordBot.InteractionModules;

public class MusicModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ConcurrentDictionary<ulong, IAudioClient> _connectedChannels = new();
    
    private readonly ILogger<MusicModule> _logger;
    private readonly IDiscordBotService _botService;
    private readonly ISettingsService _settingsService;
    private readonly IDiscordAudioService _audioService;
    
    public MusicModule(ILogger<MusicModule> logger, IDiscordAudioService audioService, IDiscordBotService botService, ISettingsService settingsService)
    {
        _logger = logger;
        _botService = botService;
        _audioService = audioService;
        _settingsService = settingsService;
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
        await MethodWrapper(async () => {
            await DeferAsync();
            await EnsureInMusicChannel();
            
            provider ??= MusicProvider.VK;
            
            if (Uri.TryCreate(query, UriKind.Absolute, out Uri? link)
                && (link.Scheme == Uri.UriSchemeHttp || link.Scheme == Uri.UriSchemeHttps)) {

                BaseMusicProviderController? controller = link.FindMusicProviderController();
                if (controller is null) {
                    await ModifyOriginalResponseAsync(m => m.Content = "***This link is not supported***");
                    return;
                }

                SocketVoiceChannel? targetChannel = GetVoiceChannelWithCurrentUser();
                if (targetChannel is null) {
                    await ModifyOriginalResponseAsync(m => m.Content = "User is not found in any of the voice channels");
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

                _audioService.RegisterChannel(Context.Guild, targetChannel.Id);
                _audioService.AddAudios(Context.Guild.Id, records);
                
                await ModifyOriginalResponseAsync((m) => m.Content = $"Queue started");
                await _audioService.PlayQueueAsync(Context.Guild.Id);
            } else {
                await ModifyOriginalResponseAsync(m => m.Content = "Searching...");
            }
        });
    }

    private async Task MethodWrapper(Func<Task> inner)
    {
        try {
            await inner();
            await Task.Delay(5000);
            await DeleteOriginalResponseAsync();
        } catch (Exception e) {
            await _botService.HandleInteractionException(e);
        }
    }

    private async Task EnsureInMusicChannel()
    {
        Guild guild = await _settingsService.GetGuildDataAsync(Context.Guild.Id);
        if (guild.MusicChannelId is null || guild.MusicMessageId is null) {
            await ModifyOriginalResponseAsync(m =>
                m.Content = "Music channel is not registered, register it with /register-music-channel");
            throw new MusicChannelNotRegisteredException();
        }

        if (Context.Channel.Id != guild.MusicChannelId.Value) {
            await ModifyOriginalResponseAsync(m =>
                m.Content = "Music player commands can only be executed in preregistered music channel");
            throw new InteractionCommandException(Context.Interaction, InteractionCommandError.Exception,
                "Attempt to execute command of a music module in the channel, " +
                "that is not registered as a music channel");
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