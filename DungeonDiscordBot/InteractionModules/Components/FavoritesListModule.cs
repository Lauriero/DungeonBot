using Discord.Interactions;

using DungeonDiscordBot.Services.Abstraction;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

namespace DungeonDiscordBot.InteractionModules.Components;

public class FavoritesListModule : MusicRequesterInteractionModule
{
    public const string REFRESH_FAVORITES_ID = "favoriteCollections-refresh";
    public const string COLLECTION_SELECT_ID = "favoriteCollections-collection-select";
    public const string PLAY_SELECTED_TRACK_ID = "favoriteCollections-play-selected";
    public const string PLAY_SELECTED_TRACK_NOW_ID = "favoriteCollections-play-selected-now";
    public const string DELETE_SELECTED_TRACK_ID = "favoriteCollections-delete-selected";
    
    private readonly IUserInterfaceService _UI;
    private readonly ILogger<FavoritesListModule> _logger;
    
    protected FavoritesListModule(ILogger<FavoritesListModule> logger, IUserInterfaceService ui) 
        : base(logger, ui)
    {
        _logger = logger;
        _UI = ui;
    }
    
    
}