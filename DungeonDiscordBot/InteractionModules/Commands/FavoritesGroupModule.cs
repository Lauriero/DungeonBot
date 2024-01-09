using Discord.Interactions;

using DungeonDiscordBot.Model;
using DungeonDiscordBot.Model.Database;
using DungeonDiscordBot.Model.MusicProviders;
using DungeonDiscordBot.MusicProvidersControllers;
using DungeonDiscordBot.Services.Abstraction;
using DungeonDiscordBot.Utilities;

using Microsoft.Extensions.Logging;

using Serilog;

using ILogger = Serilog.ILogger;

namespace DungeonDiscordBot.InteractionModules.Commands;

[Group("favorites", "Used to manage favorite collections")]
public class FavoritesGroupModule : MusicRequesterInteractionModule
{
    private readonly IUserInterfaceService _UI;
    private readonly IDataStorageService _dataStorage;
    private readonly ILogger<FavoritesGroupModule> _logger;
    
    public FavoritesGroupModule(ILogger<FavoritesGroupModule> logger, IUserInterfaceService ui, IDataStorageService dataStorage)
        : base(logger, ui)
    {
        _logger = logger;
        _UI = ui;
        _dataStorage = dataStorage;
    }

    [SlashCommand("add", 
        "Saves the music collection to favorites",
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
            AddFavoriteCollectionResult addResult = await _dataStorage.AddFavoriteMusicCollectionAsync(Context.User.Id,
                targetCollectionName, query);

            switch (addResult) {
                case AddFavoriteCollectionResult.Okay:
                    List<FavoriteMusicCollection> favorites = 
                        await _dataStorage.GetUserFavoriteMusicCollectionsAsync(Context.User.Id);
                    await ModifyOriginalResponseAsync(m =>
                        m.Content = $"Marked the music collection [{targetCollectionName}]({query}) as a favorite" +
                                    $" for the user <@{Context.User.Id}>.\n" +
                                    $"Now user favorites list contains **{favorites.Count}/" +
                                    $"{_dataStorage.MaxUserFavoritesCount}** collections.");
                    break;

                case AddFavoriteCollectionResult.OutOfSpace:
                    await ModifyOriginalResponseAsync(m => 
                        m.Content = $"User <@{Context.User.Id}> has reached the favorites limit of " +
                                    $"**{_dataStorage.MaxUserFavoritesCount}** collections.\n" +
                                    $"Remove some favorites to add the new one.");
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