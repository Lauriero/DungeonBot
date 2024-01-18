using Discord;
using Discord.Interactions;
using Discord.WebSocket;

using DungeonDiscordBot.Model;
using DungeonDiscordBot.Services.Abstraction;
using DungeonDiscordBot.Utilities;

using Microsoft.Extensions.Logging;

using RunMode = Discord.Interactions.RunMode;

namespace DungeonDiscordBot.InteractionModules.Commands;

[DefaultMemberPermissions(GuildPermission.Administrator)]
[EnabledInDm(false)]
public class ConfigModule : BaseInteractionModule<SocketInteractionContext>
{
    private readonly ILogger<ConfigModule> _logger;
    private readonly IDiscordBotService _botService;
    private readonly IDataStorageService _dataStorage;
    private readonly IDiscordAudioService _audioService;
    private readonly IUserInterfaceService _UIService;

    public ConfigModule(ILogger<ConfigModule> logger, IDiscordAudioService audioService, IDiscordBotService botService,
        IDataStorageService dataStorage, IUserInterfaceService uiService) : base(logger)
    {
        _logger = logger;
        _botService = botService;
        _audioService = audioService;
        _dataStorage = dataStorage;
        _UIService = uiService;
    }

    [SlashCommand(name: "register-music-channel",
        description: "Registers a channel that will be used to control music",
        runMode: RunMode.Async)]
    [RequireRole(UserRoles.DUNGEON_MASTER_USER_ROLE)]
    [RequireBotPermission(ChannelPermission.SendMessages | ChannelPermission.UseExternalEmojis)]
    public async Task RegisterMusicChannelAsync(
        [ChannelTypes(ChannelType.Text)]
        [Summary("channel", "The channel to control music, new messages in this channel will be removed!")]
        SocketTextChannel channel
    ) {
        await MethodWrapper(async () => {
            await _botService.EnsureBotIsReady(Context.Interaction);
            await DeferAsync();

            if (!channel.CheckChannelPermissions(ChannelPermissionsCatalogue.ForMusicControlChannel)) {
                MessageProperties missingPermissionsMessage = _UIService.GenerateMissingPermissionsMessage(
                    $"Bot should have following permissions in the channel <#{channel.Id}> in order to register it as a control channel",
                    ChannelPermissionsCatalogue.ForMusicControlChannel,
                    channel);
                await ModifyOriginalResponseAsync(m => m.ApplyMessageProperties(missingPermissionsMessage));
                return;
            }
            
            MusicPlayerMetadata metadata = _audioService.CreateMusicPlayerMetadata(Context.Guild.Id);
            ulong controlMessageId = await _UIService.CreateSongsQueueMessageAsync(Context.Guild.Id, 
                metadata, channel);
            await _dataStorage.Guilds.RegisterMusicChannel(Context.Guild.Id, channel, controlMessageId);

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
            await _dataStorage.Guilds.RegisterWelcomeChannel(Context.Guild.Id, channel.Id);
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
            await _dataStorage.Guilds.RegisterRunawayChannel(Context.Guild.Id, channel.Id);
            await ModifyOriginalResponseAsync(m => 
                m.Content = $"Registered channel <#{channel.Id}> as a runaway channel");
        });
    }
}