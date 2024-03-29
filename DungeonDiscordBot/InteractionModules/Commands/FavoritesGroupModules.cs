﻿using Discord.Interactions;

using DungeonDiscordBot.Model;
using DungeonDiscordBot.Model.Database;
using DungeonDiscordBot.Model.MusicProviders;
using DungeonDiscordBot.Services.Abstraction;
using DungeonDiscordBot.Storage.Abstraction;
using DungeonDiscordBot.Utilities;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

namespace DungeonDiscordBot.InteractionModules.Commands;

class FavoritesGroupModules
{
    [UsedImplicitly]
    [Group("favorites", "Used to manage favorite collections")]
    public class FavoritesGroupModule : BaseFavoritesModule
    {
        private readonly IUserInterfaceService _UI;
        private readonly IFavoriteCollectionsStorage _favoritesStorage;

        public FavoritesGroupModule(ILogger<FavoritesGroupModule> logger, IUserInterfaceService ui,
            IFavoriteCollectionsStorage favoritesStorage, IDiscordAudioService audioService)
            : base(logger, ui, favoritesStorage)
        {
            _UI = ui;
            _favoritesStorage = favoritesStorage;
        }
        
        [UsedImplicitly]
        [SlashCommand("list",
            "Shows favorite tracks for this user",
            runMode: RunMode.Async)]
        public async Task ListAsync()
        {
            await MethodWrapper(async () => {
                await DeferAsync(ephemeral: true);

                List<FavoriteMusicCollection> favorites = await _favoritesStorage.GetAsync(Context.User.Id);
                await ModifyOriginalResponseAsync(
                    m => m.ApplyMessageProperties(_UI.GenerateUserFavoritesMessage(favorites)));
            }, false);
        }

        [UsedImplicitly]
        [Group("add", "Used to add the collection or track to user favorites")]
        private class FavoritesAddGroupModule : BaseFavoritesModule
        {
            private readonly IDiscordAudioService _audioService;
            protected FavoritesAddGroupModule(ILogger<BaseInteractionModule<SocketInteractionContext>> logger, 
                IUserInterfaceService ui, IFavoriteCollectionsStorage favoritesStorage, IDiscordAudioService audioService) 
                : base(logger, ui, favoritesStorage)
            {
                _audioService = audioService;
            }
            
            [UsedImplicitly]
            [SlashCommand("manual", "Saves the music collection to favorites", 
                runMode: RunMode.Async)]
            public async Task ManualAsync(
                [Summary("query", "Link to a song, playlist or video")]
                string query,

                [Summary("name", "Used to specify the collection name. " +
                                 "If not specified, the determined name will be used.")]
                string? name = null)
            {
                await MethodWrapper(async () => {
                    await DeferAsync(true);

                    if (!Uri.TryCreate(query, UriKind.Absolute, out Uri? uri)
                        || uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) {

                        await ModifyOriginalResponseAsync(m => m.Content =
                            $"***{query} is not an url***");

                        return;
                    }

                    MusicCollectionResponse? collection = await FetchMusicCollectionFromUrlAsync(uri, -1, true);
                    if (collection is null || collection.IsError) {
                        return;
                    }

                    string targetCollectionName = name ?? collection.Metadata.Name;
                    await AddFavoriteCollectionAsync(targetCollectionName, query);
                }, false);
            }
            
            [UsedImplicitly]
            [SlashCommand("current-track",
                "Adds the playing track to the favorites",
                runMode: RunMode.Async)]
            public async Task CurrentTrackAsync()
            {
                await MethodWrapper(async () => {
                    await DeferAsync(true);

                    MusicPlayerMetadata metadata = _audioService.GetMusicPlayerMetadata(Context.Guild.Id);
                    if (metadata.State == MusicPlayerState.Stopped || metadata.Queue.IsEmpty) {
                        await ModifyOriginalResponseAsync(m =>
                            m.Content = "No tracks are playing now, start the queue to add current track to favorites");

                        return;
                    }

                    if (!metadata.Queue.TryPeek(out AudioQueueRecord? current)) {
                        throw new InvalidOperationException("Unable to get the first track of the queue");
                    }

                    if (current.PublicUrl is null) {
                        await ModifyOriginalResponseAsync(m =>
                            m.Content = "Current track has no public url, so it is impossible to mark it as favorite");

                        return;
                    }

                    await AddFavoriteCollectionAsync($"{current.Author} - {current.Title}", current.PublicUrl);
                }, false);
            }

            [UsedImplicitly]
            [SlashCommand("current-collection",
                "Adds the playing collection to the favorites",
                runMode: RunMode.Async)]
            public async Task CurrentCollectionAsync()
            {
                await MethodWrapper(async () => {
                    await DeferAsync(true);

                    MusicPlayerMetadata metadata = _audioService.GetMusicPlayerMetadata(Context.Guild.Id);
                    if (metadata.State == MusicPlayerState.Stopped || metadata.Queue.IsEmpty) {
                        await ModifyOriginalResponseAsync(m =>
                            m.Content = "No tracks are playing now, start the queue to add current track to favorites");

                        return;
                    }

                    if (!metadata.Queue.TryPeek(out AudioQueueRecord? current)) {
                        throw new InvalidOperationException("Unable to get the first track of the queue");
                    }

                    await AddFavoriteCollectionAsync(current.Collection.Name, current.Collection.PublicUrl);
                }, false);
            }
        }
    }

    public class BaseFavoritesModule : MusicRequesterInteractionModule
    {
        private readonly IFavoriteCollectionsStorage _favoritesStorage;
        protected BaseFavoritesModule(ILogger<BaseInteractionModule<SocketInteractionContext>> logger, 
            IUserInterfaceService ui, IFavoriteCollectionsStorage favoritesStorage) : base(logger, ui)
        {
            _favoritesStorage = favoritesStorage;
        }
        
        protected async Task AddFavoriteCollectionAsync(string name, string query)
        {
            AddFavoriteCollectionResult addResult = await _favoritesStorage.AddAsync(Context.User.Id,
                    name, query);

                switch (addResult) {
                    case AddFavoriteCollectionResult.Okay:
                        List<FavoriteMusicCollection> favorites = 
                            await _favoritesStorage.GetAsync(Context.User.Id);
                        await ModifyOriginalResponseAsync(m =>
                            m.Content = $"Marked the music collection [{name}]({query}) as a favorite" +
                                        $" for the user <@{Context.User.Id}>.\n" +
                                        $"Now user favorites list contains **{favorites.Count}/" +
                                        $"{_favoritesStorage.MaxUserFavoritesCount}** collections.");
                        break;

                    case AddFavoriteCollectionResult.OutOfSpace:
                        await ModifyOriginalResponseAsync(m => 
                            m.Content = $"User <@{Context.User.Id}> has reached the favorites limit of " +
                                        $"**{_favoritesStorage.MaxUserFavoritesCount}** collections.\n" +
                                        $"Remove some favorite collections to add the new one.");
                        break;

                    case AddFavoriteCollectionResult.AlreadyAdded:
                        await ModifyOriginalResponseAsync(m =>
                            m.Content = $"The collection {query} is already marked " +
                                        $"as the user <@{Context.User.Id}> favorite.");
                        break;

                    default:
                        await ModifyOriginalResponseAsync(m =>
                            m.Content = $"Internal error has occurred");
                        
                        throw new ArgumentOutOfRangeException(nameof(addResult),
                            "Invalid add result");
                }
        }
    }
}


