using Discord.WebSocket;

using DungeonDiscordBot.Controllers.Abstraction;
using DungeonDiscordBot.Model;
using DungeonDiscordBot.Utilities;

using Microsoft.Extensions.Logging;

namespace DungeonDiscordBot.ButtonHandlers;

public class QueueButtonHandler : IButtonHandler
{
    public string Prefix => "queue";
    
    public const string QUEUE_PAGE_BUTTON_ID_PREFIX = "queue-page";
    public const string QUEUE_HOME_PAGE_BUTTON_ID = "queue-home-page";
    
    public const string QUEUE_PREV_SONG_BUTTON_ID = "queue-previous-song";
    public const string QUEUE_TOGGLE_STATE_BUTTON_ID = "queue-toggle-state";
    public const string QUEUE_NEXT_SONG_BUTTON_ID = "queue-next-song";
    
    public const string QUEUE_NO_REPEAT_BUTTON_ID = "queue-no-repeat";
    public const string QUEUE_REPEAT_SONG_BUTTON_ID = "queue-repeat-song";
    public const string QUEUE_REPEAT_QUEUE_BUTTON_ID = "queue-repeat-queue";

    public const string QUEUE_SHUFFLE_BUTTON_ID = "queue-shuffle";
    public const string QUEUE_CLEAR_QUEUE_BUTTON_ID = "queue-clear-queue";

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
        if (component.Data.CustomId.StartsWith(QUEUE_PAGE_BUTTON_ID_PREFIX)) {
            int pageNumber = Convert.ToInt32(component.Data.CustomId[
                (QUEUE_PAGE_BUTTON_ID_PREFIX.Length + 1)..]);
            await _audioService.UpdateSongsQueueAsync(guild.Id, pageNumber);
            return;
        }
        
        switch (component.Data.CustomId) {
            case QUEUE_HOME_PAGE_BUTTON_ID:
                await _audioService.UpdateSongsQueueAsync(guild.Id, 1);
                break;
            
            // case QueueButtonHandler.QUEUE_PREV_SONG_BUTTON_ID:
            //     ;
            //     break;
            
            case QUEUE_TOGGLE_STATE_BUTTON_ID:
                if (metadata.State is MusicPlayerState.Paused or MusicPlayerState.Stopped) {
                    await _audioService.PlayQueueAsync(guild.Id);
                } else {
                    await _audioService.PauseQueueAsync(guild.Id);
                }

                break;
            
            case QUEUE_NEXT_SONG_BUTTON_ID:
                await _audioService.SkipTrackAsync(guild.Id);
                break;
            
            case QUEUE_NO_REPEAT_BUTTON_ID:
                metadata.RepeatMode = RepeatMode.NoRepeat;
                await _audioService.UpdateSongsQueueAsync(guild.Id);
                break;
            
            case QUEUE_REPEAT_SONG_BUTTON_ID:
                metadata.RepeatMode = RepeatMode.RepeatSong;
                await _audioService.UpdateSongsQueueAsync(guild.Id);
                break;
            
            case QUEUE_REPEAT_QUEUE_BUTTON_ID:
                metadata.RepeatMode = RepeatMode.RepeatQueue;
                await _audioService.UpdateSongsQueueAsync(guild.Id);
                break;
            
            case QUEUE_SHUFFLE_BUTTON_ID:
                await _audioService.ShuffleQueue(guild.Id);
                break;            
            
            case QUEUE_CLEAR_QUEUE_BUTTON_ID:
                await _audioService.ClearQueue(guild.Id);
                break;
            
            default:
                await component.ModifyOriginalResponseAsync(m => 
                    m.Content = "Command not found");
                break;
        }
    }
}
