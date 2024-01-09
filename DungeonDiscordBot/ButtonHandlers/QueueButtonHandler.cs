using Discord.WebSocket;

using DungeonDiscordBot.Model;
using DungeonDiscordBot.Services.Abstraction;

using Microsoft.Extensions.Logging;

namespace DungeonDiscordBot.ButtonHandlers;

public class QueueButtonHandler : IButtonHandler
{
    public string Prefix => "queue";
    
    public const string QUEUE_HOME_PAGE_BUTTON_ID = "queue-home-page";

    public const string QUEUE_PREV_PAGE_BUTTON_ID = "queue-previous-page";
    public const string QUEUE_NEXT_PAGE_BUTTON_ID = "queue-next-page";
    
    public const string QUEUE_PREV_SONG_BUTTON_ID = "queue-previous-song";
    public const string QUEUE_TOGGLE_STATE_BUTTON_ID = "queue-toggle-state";
    public const string QUEUE_NEXT_SONG_BUTTON_ID = "queue-next-song";
    
    public const string QUEUE_NO_REPEAT_BUTTON_ID = "queue-no-repeat";
    public const string QUEUE_REPEAT_SONG_BUTTON_ID = "queue-repeat-song";
    public const string QUEUE_REPEAT_QUEUE_BUTTON_ID = "queue-repeat-queue";

    public const string QUEUE_SHUFFLE_BUTTON_ID = "queue-shuffle";
    public const string QUEUE_CLEAR_QUEUE_BUTTON_ID = "queue-clear-queue";

    public const string DELETE_SONG_SELECT_ID = "queue-delete-select";
    public const string SWAP_SONGS_SELECT_ID_PREFIX = "queue-swap-select-";
    
    private readonly IDiscordAudioService _audioService;
    private readonly ILogger<QueueButtonHandler> _logger;
    public QueueButtonHandler(IDiscordAudioService audioService, ILogger<QueueButtonHandler> logger)
    {
        _audioService = audioService;
        _logger = logger;
    }
    
    public async Task OnButtonExecuted(SocketMessageComponent component, SocketGuild guild)
    {
        MusicPlayerMetadata metadata = _audioService.GetMusicPlayerMetadata(guild.Id);
        switch (component.Data.CustomId) {
            case QUEUE_HOME_PAGE_BUTTON_ID:
                metadata.PageNumber = 1;
                await component.ModifyOriginalResponseAsync(m => m.Content = "Switching pages...");
                await _audioService.UpdateSongsQueueAsync(guild.Id);
                break;
            
            case QUEUE_PREV_PAGE_BUTTON_ID:
                metadata.PageNumber--;
                await component.ModifyOriginalResponseAsync(m => m.Content = "Switching pages...");
                await _audioService.UpdateSongsQueueAsync(guild.Id);
                break;
            
            case QUEUE_NEXT_PAGE_BUTTON_ID:
                metadata.PageNumber++;
                await component.ModifyOriginalResponseAsync(m => m.Content = "Switching pages...");
                await _audioService.UpdateSongsQueueAsync(guild.Id);
                break;
            
            case QUEUE_PREV_SONG_BUTTON_ID:
                await _audioService.PlayPreviousTrackAsync(guild.Id);
                await component.ModifyOriginalResponseAsync(m => m.Content = "Previous song is playing");
                break;
            
            case QUEUE_TOGGLE_STATE_BUTTON_ID:
                if (metadata.State is MusicPlayerState.Paused or MusicPlayerState.Stopped) {
                    await component.ModifyOriginalResponseAsync(m => m.Content = "Play request received");
                    await _audioService.PlayQueueAsync(guild.Id);
                } else {
                    await component.ModifyOriginalResponseAsync(m => m.Content = "Pause request received");
                    await _audioService.PauseQueueAsync(guild.Id);
                }

                break;
            
            case QUEUE_NEXT_SONG_BUTTON_ID:
                await _audioService.SkipTrackAsync(guild.Id);
                await component.ModifyOriginalResponseAsync(m => m.Content = "Next song is playing");
                break;
            
            case QUEUE_NO_REPEAT_BUTTON_ID:
                metadata.RepeatMode = RepeatMode.NoRepeat;
                await _audioService.UpdateSongsQueueAsync(guild.Id);
                await component.ModifyOriginalResponseAsync(m => m.Content = "Repeat mode is switched");
                break;
            
            case QUEUE_REPEAT_SONG_BUTTON_ID:
                metadata.RepeatMode = RepeatMode.RepeatSong;
                await _audioService.UpdateSongsQueueAsync(guild.Id);
                await component.ModifyOriginalResponseAsync(m => m.Content = "Repeat mode is switched");
                break;
            
            case QUEUE_REPEAT_QUEUE_BUTTON_ID:
                metadata.RepeatMode = RepeatMode.RepeatQueue;
                await _audioService.UpdateSongsQueueAsync(guild.Id);
                await component.ModifyOriginalResponseAsync(m => m.Content = "Repeat mode is switched");
                break;
            
            case QUEUE_SHUFFLE_BUTTON_ID:
                await _audioService.ShuffleQueue(guild.Id);
                await component.ModifyOriginalResponseAsync(m => m.Content = "Queue is shuffled");
                break;            
            
            case QUEUE_CLEAR_QUEUE_BUTTON_ID:
                await _audioService.ClearQueue(guild.Id);
                await component.ModifyOriginalResponseAsync(m => m.Content = "Queue is cleared");
                break;
            
            default:
                await component.ModifyOriginalResponseAsync(m => 
                    m.Content = "Command not found");
                break;
        }

    }
}
