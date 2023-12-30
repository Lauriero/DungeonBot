using Discord.Interactions;

using Microsoft.Extensions.Logging;

namespace DungeonDiscordBot.InteractionModules.Components;

public class PlaybackHistoryModule : InteractionModuleBase<SocketInteractionContext>
{
    public const string TRACK_SELECT_ID = "playback-track-select";
    public const string HISTORY_REFRESH_ID = "playback-refresh";
    public const string PLAY_SELECTED_TRACK_ID = "playback-play-selected";
    public const string PLAY_SELECTED_TRACK_NOW_ID = "playback-play-selected-now";

    private readonly ILogger<PlaybackHistoryModule> _logger;
    
    public PlaybackHistoryModule(ILogger<PlaybackHistoryModule> logger)
    {
        _logger = logger;
    }
    
    [ComponentInteraction(TRACK_SELECT_ID, runMode: RunMode.Async)]
    public async Task TrackSelectAsync(string[] selectedRoles)
    {
        await DeferAsync();
        
        _logger.LogInformation($"Tracks {string.Join(", ", selectedRoles)} selected with the playback history embed " +
                               $"by the {Context.User.Username}@{Context.User.Id} " +
                               $"in the guild {Context.Guild.Name}@{Context.Guild.Id}");

        await DeleteOriginalResponseAsync();
    }

    [ComponentInteraction(PLAY_SELECTED_TRACK_ID, runMode: RunMode.Async)]
    public async Task PlaySelectedAsync()
    {
        await DeferAsync();
        
        _logger.LogInformation("Command to play selected track is received through the playback history embed " +
                               $"by the {Context.User.Username}@{Context.User.Id} " +
                               $"in the guild {Context.Guild.Name}@{Context.Guild.Id}");

        await DeleteOriginalResponseAsync();
    }
    
    [ComponentInteraction(PLAY_SELECTED_TRACK_NOW_ID, runMode: RunMode.Async)]
    public async Task PlaySelectedNowAsync()
    {
        await DeferAsync();
        
        _logger.LogInformation("Command to play selected track NOW is received through the playback history embed " +
                               $"by the {Context.User.Username}@{Context.User.Id} " +
                               $"in the guild {Context.Guild.Name}@{Context.Guild.Id}");

        await DeleteOriginalResponseAsync();
    }
    
    [ComponentInteraction(HISTORY_REFRESH_ID, runMode: RunMode.Async)]
    public async Task RefreshAsync()
    {
        await DeferAsync();
        
        _logger.LogInformation("Command to refresh history is received through the playback history embed " +
                               $"by the {Context.User.Username}@{Context.User.Id} " +
                               $"in the guild {Context.Guild.Name}@{Context.Guild.Id}");

        await DeleteOriginalResponseAsync();
    }
}