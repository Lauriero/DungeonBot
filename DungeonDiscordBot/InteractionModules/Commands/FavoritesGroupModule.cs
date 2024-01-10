using Discord.Interactions;

using DungeonDiscordBot.Model;
using DungeonDiscordBot.Model.Database;
using DungeonDiscordBot.Model.MusicProviders;
using DungeonDiscordBot.MusicProvidersControllers;
using DungeonDiscordBot.Services.Abstraction;
using DungeonDiscordBot.Storage.Abstraction;
using DungeonDiscordBot.Utilities;

using Microsoft.Extensions.Logging;

using Serilog;

using ILogger = Serilog.ILogger;

namespace DungeonDiscordBot.InteractionModules.Commands;

[Group("favorites", "Used to manage favorite collections")]
public class FavoritesGroupModule : MusicRequesterInteractionModule
{
    private readonly IUserInterfaceService _UI;
    private readonly IFavoriteCollectionsStorage _favoritesStorage;
    private readonly ILogger<FavoritesGroupModule> _logger;
    
    public FavoritesGroupModule(ILogger<FavoritesGroupModule> logger, IUserInterfaceService ui, 
        IFavoriteCollectionsStorage favoritesStorage)
        : base(logger, ui)
    {
        _logger = logger;
        _UI = ui;
        _favoritesStorage = favoritesStorage;
    }

    [SlashCommand("add", 
        "Saves the music collection to favoriteCollections",
        runMode: RunMode.Async)]
    public async Task AddAsync(
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
            
            MusicCollectionResponse? collection = await FetchMusicCollectionFromUrlAsync(uri, 0, true);
            if (collection is null || collection.IsError) {
                return;
            }

            string targetCollectionName = name ?? collection.Name;
            AddFavoriteCollectionResult addResult = await _favoritesStorage.AddAsync(Context.User.Id,
                targetCollectionName, query);

            switch (addResult) {
                case AddFavoriteCollectionResult.Okay:
                    List<FavoriteMusicCollection> favorites = 
                        await _favoritesStorage.GetAsync(Context.User.Id);
                    await ModifyOriginalResponseAsync(m =>
                        m.Content = $"Marked the music collection [{targetCollectionName}]({query}) as a favorite" +
                                    $" for the user <@{Context.User.Id}>.\n" +
                                    $"Now user favoriteCollections list contains **{favorites.Count}/" +
                                    $"{_favoritesStorage.MaxUserFavoritesCount}** collections.");
                    break;

                case AddFavoriteCollectionResult.OutOfSpace:
                    await ModifyOriginalResponseAsync(m => 
                        m.Content = $"User <@{Context.User.Id}> has reached the favoriteCollections limit of " +
                                    $"**{_favoritesStorage.MaxUserFavoritesCount}** collections.\n" +
                                    $"Remove some favoriteCollections to add the new one.");
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
        }, false);
    }
}