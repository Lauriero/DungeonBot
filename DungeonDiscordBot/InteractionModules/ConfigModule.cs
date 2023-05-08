using Discord.Interactions;
using Discord.WebSocket;

using DungeonDiscordBot.Controllers;
using DungeonDiscordBot.Controllers.Abstraction;

using Microsoft.Extensions.Logging;

using RunMode = Discord.Interactions.RunMode;

namespace DungeonDiscordBot.InteractionModules;

public class ConfigModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ILogger<ConfigModule> _logger;
    private readonly IDiscordBotService _botService;
    private readonly ISettingsService _settingsService;
    private readonly IDiscordAudioService _audioService;
    private readonly IUserInterfaceService _UIService;
    
    public ConfigModule(ILogger<ConfigModule> logger, IDiscordAudioService audioService, IDiscordBotService botService, ISettingsService settingsService, IUserInterfaceService uiService)
    {
        _logger = logger;
        _botService = botService;
        _audioService = audioService;
        _settingsService = settingsService;
        _UIService = uiService;
    }
    
    [SlashCommand(
        name: "register-music-channel",
        description: "Registers a channel that will be used to control music",
        runMode: RunMode.Async)]
    public async Task RegisterMusicAsync(
        [Summary("channel", "The channel to control music, new messages in this channel will be removed!")]
        SocketTextChannel channel
    ) {
        await MethodWrapper(async () => {
            await DeferAsync();
            await _UIService.CreateSongsQueueMessageAsync(Context.Guild.Id, 
                _audioService.GetQueue(Context.Guild.Id), 
                _audioService.CreateMusicPlayerMetadata(Context.Guild.Id), channel);
            await ModifyOriginalResponseAsync(m => 
                m.Content = $"Registered channel <#{channel.Id}> as music control channel");
        });
    }
    
    private async Task MethodWrapper(Func<Task> inner)
    {
        try {
            await inner();
        } catch (Exception e) {
            await _botService.HandleInteractionException(e);
        }
    }
}