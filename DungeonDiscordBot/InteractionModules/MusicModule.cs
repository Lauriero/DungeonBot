using System.Collections.Concurrent;

using Discord;
using Discord.Audio;
using Discord.Interactions;
using Discord.WebSocket;

using DungeonDiscordBot.AutocompleteHandlers;
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
    private readonly IDataStorageService _dataStorageService;
    private readonly IDiscordAudioService _audioService;
    private readonly IUserInterfaceService _UIService;
    
    private readonly ChannelPermission[] _voiceChannelPermissions = {
        ChannelPermission.Speak,
    };
    
    public MusicModule(ILogger<MusicModule> logger, IDiscordAudioService audioService, IDiscordBotService botService, IDataStorageService dataStorageService, IUserInterfaceService uiService)
    {
        _logger = logger;
        _botService = botService;
        _audioService = audioService;
        _dataStorageService = dataStorageService;
        _UIService = uiService;
    }

    [SlashCommand(
        name:        "play",
        description: "Plays song or playlist from the link or search request", 
        runMode:     RunMode.Async)]
    public async Task PlayAsync(
        [Summary("query", "Link to a song, playlist, video or a search query")]
        [Autocomplete(typeof(QueryAutocompleteHandler))]
        string query,
        
        [Summary("provider", "Name of the music providerController the search will be performed with (VK default)")]
        MusicProvider? provider = null,
        
        [Summary("quantity", "Number of tracks that should be fetched")]
        int quantity = -1,
        
        [Summary("now", "Flag to put the fetched songs in the head of the playlist")]
        bool now = false
    )
    {
        await MethodWrapper(async () => {
            await _botService.EnsureBotIsReady(Context.Interaction);
            await DeferAsync();
            await EnsureInMusicChannel();
            
            SocketVoiceChannel? targetChannel = GetVoiceChannelWithCurrentUser();
            if (targetChannel is null) {
                await ModifyOriginalResponseAsync(m => m.Content = "User is not found in any of the voice channels");
                return;
            }

            if (!targetChannel.CheckChannelPermissions(_voiceChannelPermissions)) {
                MessageProperties missingPermissionsMessage = _UIService.GenerateMissingPermissionsMessage(
                    $"Bot should have following permissions in the channel <#{targetChannel.Id}> in order to play music",
                    _voiceChannelPermissions,
                    targetChannel);
                await ModifyOriginalResponseAsync(m => m.ApplyMessageProperties(missingPermissionsMessage));
                return;
            }
            
            provider ??= MusicProvider.VK;
            MusicCollectionResponse collection;
            string message = "";
            if (Uri.TryCreate(query, UriKind.Absolute, out Uri? link)
                && (link.Scheme == Uri.UriSchemeHttp || link.Scheme == Uri.UriSchemeHttps)) {

                BaseMusicProviderController? controller = link.FindMusicProviderController();
                if (controller is null) {
                    await ModifyOriginalResponseAsync(m => m.Content = "***This link is not supported***");
                    return;
                }

                controller.AudiosProcessingStarted += audiosCount => {
                    Task.Run(async () =>
                        await ModifyOriginalResponseAsync(m => 
                            m.Content = $"Processing 0/{audiosCount}"));
                };

                controller.AudiosProcessingProgressed += (audiosProcessed, audiosCount) => {
                    Task.Run(async () =>
                        await ModifyOriginalResponseAsync(m => 
                            m.Content = $"Processing {audiosProcessed}/{audiosCount}"));
                };

                controller.AudiosProcessed += audiosProcessed => {
                    Task.Run(async () =>
                        await ModifyOriginalResponseAsync(m => 
                            m.Content = $"{audiosProcessed} audios were processed"));
                };
                
                collection = await controller.GetAudiosFromLinkAsync(link, quantity);
                if (collection.IsError) {
                    _logger.LogInformation($"Error while getting music from {collection.Provider.Name} music provider " +
                                           $"[guildId: {Context.Guild.Id}; query: {query}]: " +
                                           $"{collection.ErrorType} - {collection.ErrorMessage}");
                    switch (collection.ErrorType) {
                        case MusicResponseErrorType.PermissionDenied:
                            await ModifyOriginalResponseAsync((m) 
                                => m.Content = $"Permission to audio was denied");
                            return;
                        
                        case MusicResponseErrorType.NoAudioFound:
                            await ModifyOriginalResponseAsync((m) 
                                => m.Content = $"No audio was found by the requested url");
                            return;
                        
                        case MusicResponseErrorType.LinkNotSupported:
                            await ModifyOriginalResponseAsync((m) 
                                => m.Content = $"Bot is not able to parse this type of link");
                            return;
                        default:
                            return;
                    }
                }

                message = $"**{collection.Audios.Count()}** tracks from {collection.Name} were added to the queue";
            } else {
                await ModifyOriginalResponseAsync(m => m.Content = "Searching...");
                collection = await provider.Value.GetAudioFromSearchQueryAsync(query);
                if (collection.IsError) {
                    switch (collection.ErrorType) {
                        case MusicResponseErrorType.NoAudioFound:
                            await ModifyOriginalResponseAsync(m => m.Content = "Nothing was found");
                            return;
                        
                        default:
                            await ModifyOriginalResponseAsync(m => 
                                m.Content = "Unhandled error has occured while searching");
                            return;  
                    }
                }
                
                message = $"Song ***{collection.Name}*** was added to the queue";
                await ModifyOriginalResponseAsync(m => 
                    m.Content = "Song is found");
            }

            await _dataStorageService.RegisterMusicQueryAsync(Context.Guild.Id, collection.Name, query);
            _audioService.RegisterChannel(Context.Guild, targetChannel.Id);
            _audioService.AddAudios(Context.Guild.Id, collection.Audios, now);
            await _audioService.PlayQueueAsync(Context.Guild.Id, message);
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
        Guild guild = await _dataStorageService.GetGuildDataAsync(Context.Guild.Id);
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