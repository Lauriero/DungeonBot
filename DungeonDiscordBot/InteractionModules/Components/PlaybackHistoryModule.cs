using System.Collections.Concurrent;

using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;

using DungeonDiscordBot.Model;
using DungeonDiscordBot.Model.MusicProviders;
using DungeonDiscordBot.MusicProvidersControllers;
using DungeonDiscordBot.Services.Abstraction;
using DungeonDiscordBot.Utilities;

using Microsoft.Extensions.Logging;

namespace DungeonDiscordBot.InteractionModules.Components;

public class PlaybackHistoryModule : MusicRequesterInteractionModule
{
    public const string REFRESH_HISTORY_ID = "playback-refresh";
    public const string TRACK_SELECT_ID = "playback-track-select";
    public const string PLAY_SELECTED_TRACK_ID = "playback-play-selected";
    public const string PLAY_SELECTED_TRACK_NOW_ID = "playback-play-selected-now";

    private readonly IDataStorageService _dataStorage;
    private readonly IUserInterfaceService _UI;
    private readonly IDiscordAudioService _audioService;
    private readonly ILogger<PlaybackHistoryModule> _logger;

    public PlaybackHistoryModule(ILogger<PlaybackHistoryModule> logger, IUserInterfaceService ui, 
        IDiscordAudioService audioService, IDataStorageService dataStorage) 
        : base(logger, ui)
    {
        _logger = logger;
        _UI = ui;
        _audioService = audioService;
        _dataStorage = dataStorage;
    }

    [ComponentInteraction(REFRESH_HISTORY_ID, runMode: RunMode.Async)]
    public async Task RefreshAsync()
    {
        await MethodWrapper(async () => {
            await DeferAsync();
            
            string? selectedTrackUri = GetSelectedTrackUri();
            MusicPlayerMetadata metadata = _audioService.GetMusicPlayerMetadata(Context.Guild.Id);
            if (metadata.PreviousTracks.All(t => t.PublicUrl != selectedTrackUri)) {
                if (Context.Interaction is not SocketMessageComponent componentInteraction) {
                    throw new Exception($"Component interaction {TRACK_SELECT_ID} was created, " +
                                        "but an interaction within context was not a SocketMessageComponent interaction");
                }

                _dataStorage.HistoryMessageSelectedOptions.TryRemove(componentInteraction.Message.Id, out string? _);
            }
            
            await ModifyOriginalResponseAsync(m => m.ApplyMessageProperties(
                _UI.GenerateTrackHistoryMessage(
                    metadata.PreviousTracks, 
                    selectedTrackUri)));
        }, false);
    }
    
    [ComponentInteraction(TRACK_SELECT_ID, runMode: RunMode.Async)]
    public async Task TrackSelectAsync(string[] selectedRoles)
    {
        await MethodWrapper(async () => {
            await DeferAsync();
            
            _logger.LogInformation($"Tracks {string.Join(", ", selectedRoles)} selected with the playback history embed " +
                                   $"by the {Context.User.Username}@{Context.User.Id} " +
                                   $"in the guild {Context.Guild.Name}@{Context.Guild.Id}");


            if (Context.Interaction is not SocketMessageComponent componentInteraction) {
                throw new Exception($"Component interaction {TRACK_SELECT_ID} was created, " +
                                    "but an interaction within context was not a SocketMessageComponent interaction");
            }

            ulong messageId = componentInteraction.Message.Id;
            _dataStorage.HistoryMessageSelectedOptions.AddOrUpdate(messageId, 
                selectedRoles.First(), 
                (_, _) => selectedRoles.First());

            MusicPlayerMetadata metadata = _audioService.GetMusicPlayerMetadata(Context.Guild.Id);
            await ModifyOriginalResponseAsync(m => m.ApplyMessageProperties(
                _UI.GenerateTrackHistoryMessage(
                    metadata.PreviousTracks, 
                    selectedRoles.First())));
        }, false);
    }

    [ComponentInteraction(PLAY_SELECTED_TRACK_ID, runMode: RunMode.Async)]
    public async Task PlaySelectedAsync()
    {
        await MethodWrapper(async () => {
            await DeferAsync();
            _logger.LogInformation("Command to play selected track is received through the playback history embed " +
                                   $"by the {Context.User.Username}@{Context.User.Id} " +
                                   $"in the guild {Context.Guild.Name}@{Context.Guild.Id}");

           string? selectedTrackUri = GetSelectedTrackUri();
            if (selectedTrackUri is not null) {
                await PlayAsync(new Uri(selectedTrackUri, UriKind.Absolute), false);
            } else {
                await SendMessageWithDeletion(new MessageProperties {
                    Content = $"<@{Context.User.Id}> select a track to queue it up"
                });
            }
            
            await ModifyOriginalResponseAsync(m => m.ApplyMessageProperties(
                _UI.GenerateTrackHistoryMessage(
                    _audioService.GetMusicPlayerMetadata(Context.Guild.Id).PreviousTracks, 
                    selectedTrackUri)));
        }, false);
        
    }
    
    [ComponentInteraction(PLAY_SELECTED_TRACK_NOW_ID, runMode: RunMode.Async)]
    public async Task PlaySelectedNowAsync()
    {
        await MethodWrapper(async () => {
            await DeferAsync();
            _logger.LogInformation("Command to play selected track NOW is received through the playback history embed " +
                                   $"by the {Context.User.Username}@{Context.User.Id} " +
                                   $"in the guild {Context.Guild.Name}@{Context.Guild.Id}");

            string? selectedTrackUri = GetSelectedTrackUri();
            if (selectedTrackUri is not null) {
                await PlayAsync(new Uri(selectedTrackUri, UriKind.Absolute), true);
            } else {
                await SendMessageWithDeletion(new MessageProperties {
                    Content = $"<@{Context.User.Id}> should select a track to queue it up"
                });
            }
            
            await ModifyOriginalResponseAsync(m => m.ApplyMessageProperties(
                _UI.GenerateTrackHistoryMessage(
                    _audioService.GetMusicPlayerMetadata(Context.Guild.Id).PreviousTracks, 
                    selectedTrackUri)));
        }, false);
    }

    private string? GetSelectedTrackUri()
    {
        if (Context.Interaction is not SocketMessageComponent componentInteraction) {
            throw new Exception($"Component interaction {TRACK_SELECT_ID} was created, " +
                                "but an interaction within context was not a SocketMessageComponent interaction");
        }

        _dataStorage.HistoryMessageSelectedOptions.TryGetValue(componentInteraction.Message.Id, out string? link);
        return link;
    }  

    private async Task PlayAsync(Uri trackUri, bool now)
    {
        MusicCollectionResponse? collection = await FetchMusicCollectionFromUrlAsync(trackUri, 1, false);
        if (collection is null || collection.IsError) {
            return;
        }
        
        await _audioService.AddAudios(Context.Guild.Id, collection.Audios, now);
        MusicPlayerMetadata playerMetadata = _audioService.GetMusicPlayerMetadata(Context.Guild.Id);
        if (playerMetadata.State != MusicPlayerState.Stopped) {
            return;
        }
        
        SocketVoiceChannel? targetChannel = Context.GetVoiceChannelWithCurrentUser();
        if (targetChannel is null) {
            await SendMessageWithDeletion(new MessageProperties {Content = "User is not found in any of the voice channels"});
            return;
        }

        if (!targetChannel.CheckChannelPermissions(ChannelPermissionsCatalogue.ForVoiceChannel)) {
            MessageProperties missingPermissionsMessage = _UI.GenerateMissingPermissionsMessage(
                $"Bot should have following permissions in the channel <#{targetChannel.Id}> in order to play music",
                ChannelPermissionsCatalogue.ForVoiceChannel,
                targetChannel);
            await SendMessageWithDeletion(missingPermissionsMessage);
            return;
        }

        playerMetadata.VoiceChannel = targetChannel;
        await _audioService.PlayQueueAsync(Context.Guild.Id);
    }

    private async Task SendMessageWithDeletion(MessageProperties properties, RequestOptions? options = null)
    {
        RestUserMessage message = await Context.Channel.SendMessageAsync(properties, options);
        ThreadPool.QueueUserWorkItem(async m => {
            await Task.Delay(15000);
            await m.DeleteAsync();
        }, message, true);
    }
}