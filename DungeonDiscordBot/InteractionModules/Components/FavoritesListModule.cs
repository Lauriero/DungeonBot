using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;

using DungeonDiscordBot.Model;
using DungeonDiscordBot.Model.Database;
using DungeonDiscordBot.Model.MusicProviders;
using DungeonDiscordBot.Services.Abstraction;
using DungeonDiscordBot.Utilities;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

namespace DungeonDiscordBot.InteractionModules.Components;

public class FavoritesListModule : MusicRequesterInteractionModule
{
    public const string REFRESH_FAVORITES_ID = "favorites-refresh";
    public const string COLLECTION_SELECT_ID = "favorites-collection-select";
    public const string PLAY_SELECTED_TRACK_ID = "favorites-play-selected";
    public const string PLAY_SELECTED_TRACK_NOW_ID = "favorites-play-selected-now";
    public const string DELETE_SELECTED_TRACK_ID = "favorites-delete-selected";
    
    private readonly IUserInterfaceService _UI;
    private readonly ILogger<FavoritesListModule> _logger;
    private readonly IDiscordAudioService _audioService;
    private readonly IDataStorageService _dataStorage;
    
    protected FavoritesListModule(ILogger<FavoritesListModule> logger, IUserInterfaceService ui, IDiscordAudioService audioService, IDataStorageService dataStorage) 
        : base(logger, ui)
    {
        _logger = logger;
        _UI = ui;
        _audioService = audioService;
        _dataStorage = dataStorage;
    }
    
    [ComponentInteraction(REFRESH_FAVORITES_ID, runMode: RunMode.Async)]
    public async Task RefreshAsync()
    {
        await MethodWrapper(async () => {
            await DeferAsync();
            
            string? selectedCollectionUri = GetSelectedCollectionUri();
            List<FavoriteMusicCollection> favorites = await _dataStorage.FavoriteCollections.GetAsync(Context.User.Id);
            if (favorites.All(c => c.Query != selectedCollectionUri)) {
                if (Context.Interaction is not SocketMessageComponent componentInteraction) {
                    throw new Exception($"Component interaction {COLLECTION_SELECT_ID} was created, " +
                                        "but an interaction within context was not a SocketMessageComponent interaction");
                }

                _dataStorage.SelectedOptions.FavoritesMessage.TryRemove(componentInteraction.Message.Id, out string? _);
            }
            
            await ModifyOriginalResponseAsync(m => m.ApplyMessageProperties(
                _UI.GenerateUserFavoritesMessage(favorites, selectedCollectionUri)));
        }, false);
    }
    
    [ComponentInteraction(COLLECTION_SELECT_ID, runMode: RunMode.Async)]
    public async Task CollectionSelectAsync(string[] selectedCollections)
    {
        await MethodWrapper(async () => {
            await DeferAsync();
            
            _logger.LogInformation($"Collections {string.Join(", ", selectedCollections)} selected " +
                                   $"with the favorites embed " +
                                   $"by the {Context.User.Username}@{Context.User.Id} " +
                                   $"in the guild {Context.Guild.Name}@{Context.Guild.Id}");


            if (Context.Interaction is not SocketMessageComponent componentInteraction) {
                throw new Exception($"Component interaction {COLLECTION_SELECT_ID} was created, " +
                                    "but an interaction within context was not a SocketMessageComponent interaction");
            }

            ulong messageId = componentInteraction.Message.Id;
            _dataStorage.SelectedOptions.FavoritesMessage.AddOrUpdate(messageId, 
                selectedCollections.First(), 
                (_, _) => selectedCollections.First());

            List<FavoriteMusicCollection> favorites = await _dataStorage.FavoriteCollections.GetAsync(Context.User.Id);
            await ModifyOriginalResponseAsync(m => m.ApplyMessageProperties(
                _UI.GenerateUserFavoritesMessage(favorites, selectedCollections.First())));
        }, false);
    }

    [ComponentInteraction(PLAY_SELECTED_TRACK_ID, runMode: RunMode.Async)]
    public async Task PlaySelectedAsync()
    {
        await MethodWrapper(async () => {
            await DeferAsync();
            _logger.LogInformation("Command to play selected collection is received " +
                                   "through the favorites embed " +
                                   $"by the {Context.User.Username}@{Context.User.Id} " +
                                   $"in the guild {Context.Guild.Name}@{Context.Guild.Id}");

            string? selectedCollectionUri = GetSelectedCollectionUri();
            if (selectedCollectionUri is not null) {
                await PlayAsync(new Uri(selectedCollectionUri, UriKind.Absolute), false);
            } else {
                await SendMessageWithDeletion(new MessageProperties {
                    Content = $"<@{Context.User.Id}> should select a collection to queue it up"
                });
            }
            
            List<FavoriteMusicCollection> favorites = await _dataStorage.FavoriteCollections.GetAsync(Context.User.Id);
            await ModifyOriginalResponseAsync(m => m.ApplyMessageProperties(
                _UI.GenerateUserFavoritesMessage(favorites, selectedCollectionUri)));
        }, false);
        
    }
    
    [ComponentInteraction(PLAY_SELECTED_TRACK_NOW_ID, runMode: RunMode.Async)]
    public async Task PlaySelectedNowAsync()
    {
        await MethodWrapper(async () => {
            await DeferAsync();
            _logger.LogInformation("Command to play selected collection NOW is received " +
                                   "through the favorites embed " +
                                   $"by the {Context.User.Username}@{Context.User.Id} " +
                                   $"in the guild {Context.Guild.Name}@{Context.Guild.Id}");

            string? selectedCollectionUri = GetSelectedCollectionUri();
            if (selectedCollectionUri is not null) {
                await PlayAsync(new Uri(selectedCollectionUri, UriKind.Absolute), true);
            } else {
                await SendMessageWithDeletion(new MessageProperties {
                    Content = $"<@{Context.User.Id}> should select a collection to queue it up"
                });
            }
            
            List<FavoriteMusicCollection> favorites = await _dataStorage.FavoriteCollections.GetAsync(Context.User.Id);
            await ModifyOriginalResponseAsync(m => m.ApplyMessageProperties(
                _UI.GenerateUserFavoritesMessage(favorites, selectedCollectionUri)));
        }, false);
    }

    [ComponentInteraction(DELETE_SELECTED_TRACK_ID, runMode: RunMode.Async)]
    public async Task DeleteAsync()
    {
        await MethodWrapper(async () => {
            await DeferAsync();
            string? selectedCollectionUri = GetSelectedCollectionUri();
            if (selectedCollectionUri is not null) {
                await _dataStorage.FavoriteCollections.DeleteAsync(Context.User.Id, selectedCollectionUri);
            } else {
                await SendMessageWithDeletion(new MessageProperties {
                    Content = $"<@{Context.User.Id}> should select a collection to queue it up"
                });
            }
            
            List<FavoriteMusicCollection> favorites = await _dataStorage.FavoriteCollections.GetAsync(Context.User.Id);
            await ModifyOriginalResponseAsync(m => m.ApplyMessageProperties(
                _UI.GenerateUserFavoritesMessage(favorites, selectedCollectionUri)));
        }, false);
    }
    
    private string? GetSelectedCollectionUri()
    {
        if (Context.Interaction is not SocketMessageComponent componentInteraction) {
            throw new Exception($"Component interaction {COLLECTION_SELECT_ID} was created, " +
                                "but an interaction within context was not a SocketMessageComponent interaction");
        }

        _dataStorage.SelectedOptions.FavoritesMessage.TryGetValue(componentInteraction.Message.Id, out string? link);
        return link;
    }  

    private async Task PlayAsync(Uri collectionUri, bool now)
    {
        MusicCollectionResponse? collection = await FetchMusicCollectionFromUrlAsync(collectionUri, -1, false);
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