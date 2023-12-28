using Discord;
using Discord.Interactions;
using Discord.WebSocket;

using DungeonDiscordBot.Controllers;
using DungeonDiscordBot.Controllers.Abstraction;
using DungeonDiscordBot.Utilities;

using Microsoft.Extensions.Logging;

using RunMode = Discord.Interactions.RunMode;

namespace DungeonDiscordBot.InteractionModules;

[DefaultMemberPermissions(GuildPermission.Administrator)]
[EnabledInDm(false)]
public class ConfigModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ILogger<ConfigModule> _logger;
    private readonly IDiscordBotService _botService;
    private readonly IDataStorageService _dataStorageService;
    private readonly IDiscordAudioService _audioService;
    private readonly IUserInterfaceService _UIService;

    private readonly ChannelPermission[] _musicControlChannelPermissions = {
        ChannelPermission.SendMessages,
        ChannelPermission.ManageMessages,
        ChannelPermission.ReadMessageHistory,
        ChannelPermission.UseExternalEmojis,
        ChannelPermission.EmbedLinks
    };

    public ConfigModule(ILogger<ConfigModule> logger, IDiscordAudioService audioService, IDiscordBotService botService,
        IDataStorageService dataStorageService, IUserInterfaceService uiService)
    {
        _logger = logger;
        _botService = botService;
        _audioService = audioService;
        _dataStorageService = dataStorageService;
        _UIService = uiService;
    }

    [SlashCommand(name: "register-music-channel",
        description: "Registers a channel that will be used to control music",
        runMode: RunMode.Async)]
    [RequireRole(UserRoles.DUNGEON_MASTER_USER_ROLE)]
    [RequireBotPermission(ChannelPermission.SendMessages | ChannelPermission.UseExternalEmojis)]
    public async Task RegisterMusicChannelAsync(
        [Summary("channel", "The channel to control music, new messages in this channel will be removed!")]
        SocketTextChannel channel
    ) {
        await MethodWrapper(async () => {
            await _botService.EnsureBotIsReady(Context.Interaction);
            await DeferAsync();

            if (!channel.CheckChannelPermissions(_musicControlChannelPermissions)) {
                MessageProperties missingPermissionsMessage = _UIService.GenerateMissingPermissionsMessage(
                    $"Bot should have following permissions in the channel <#{channel.Id}> in order to register it as a control channel",
                    _musicControlChannelPermissions,
                    channel);
                await ModifyOriginalResponseAsync(m => m.ApplyMessageProperties(missingPermissionsMessage));
                return;
            }
            
            await _UIService.CreateSongsQueueMessageAsync(Context.Guild.Id, 
                _audioService.GetQueue(Context.Guild.Id), 
                _audioService.CreateMusicPlayerMetadata(Context.Guild.Id), channel);
            await ModifyOriginalResponseAsync(m => 
                m.Content = $"Registered channel <#{channel.Id}> as music control channel");
        });
    }

    [SlashCommand(name: "register-welcome-channel",
        description: "Registers a channel that will be used to notify about new users",
        runMode: RunMode.Async)]
    [RequireRole(UserRoles.DUNGEON_MASTER_USER_ROLE)]
    [RequireBotPermission(ChannelPermission.SendMessages | ChannelPermission.UseExternalEmojis)]
    public async Task RegisterWelcomeChannelAsync(
        [Summary("channel", "The welcome channel")]
        SocketTextChannel channel)
    {
        await MethodWrapper(async () => {
            await DeferAsync();
            await _dataStorageService.RegisterWelcomeChannel(Context.Guild.Id, channel.Id);
            await ModifyOriginalResponseAsync(m => 
                m.Content = $"Registered channel <#{channel.Id}> as a welcome channel");
        });
    }
    
    [SlashCommand(name: "register-runaway-channel",
        description: "Registers a channel that will be used to notify about left users",
        runMode: RunMode.Async)]
    [RequireRole(UserRoles.DUNGEON_MASTER_USER_ROLE)]
    [RequireBotPermission(ChannelPermission.SendMessages | ChannelPermission.UseExternalEmojis)]
    public async Task RegisterRunawayChannelAsync(
        [Summary("channel", "The runaway channel")]
        SocketTextChannel channel)
    {
        await MethodWrapper(async () => {
            await DeferAsync();
            await _dataStorageService.RegisterRunawayChannel(Context.Guild.Id, channel.Id);
            await ModifyOriginalResponseAsync(m => 
                m.Content = $"Registered channel <#{channel.Id}> as a runaway channel");
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