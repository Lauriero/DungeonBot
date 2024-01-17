using System.Collections.Concurrent;
using System.Reflection;

using Discord;
using Discord.Interactions;
using Discord.WebSocket;

using DungeonDiscordBot.ButtonHandlers;
using DungeonDiscordBot.Exceptions;
using DungeonDiscordBot.Model;
using DungeonDiscordBot.Model.Database;
using DungeonDiscordBot.Model.MusicProviders;
using DungeonDiscordBot.MusicProvidersControllers;
using DungeonDiscordBot.Services.Abstraction;
using DungeonDiscordBot.Settings;
using DungeonDiscordBot.Storage.Abstraction;
using DungeonDiscordBot.TypeConverters;
using DungeonDiscordBot.Utilities;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DungeonDiscordBot.Services
{
    public class DiscordBotService : IDiscordBotService
    {
        public int InitializationPriority => 10;

        private readonly ILogger<IDiscordBotService> _logger; 
        private readonly AppSettings _settings;
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _interactions;
        private readonly IGuildsStorage _dataStorage;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDiscordAudioService _audioService;
        private readonly IUserInterfaceService _UIService;
        private readonly ConcurrentDictionary<string, IButtonHandler> _buttonHandlers;
        
        public DiscordBotService(IServiceProvider serviceProvider, ILogger<IDiscordBotService> logger, 
            DiscordSocketClient client, InteractionService interactions, IOptions<AppSettings> settings, 
            IGuildsStorage dataStorage, IDiscordAudioService audioService, IUserInterfaceService uiService)
        {
            _logger = logger;
            _client = client;
            _settings = settings.Value;
            _interactions = interactions;
            _dataStorage = dataStorage;
            _audioService = audioService;
            _UIService = uiService;

            _serviceProvider = serviceProvider;
            _buttonHandlers = new ConcurrentDictionary<string, IButtonHandler>(
                _serviceProvider.GetAllServices<IButtonHandler>().Select(handler =>
                    new KeyValuePair<string, IButtonHandler>(handler.Prefix, handler)));
        }


        public async Task InitializeAsync()
        {
            _logger.LogInformation("Initializing Discord service...");

            _client.JoinedGuild += async guild => {
                await _dataStorage.RegisterGuild(guild.Id, guild.Name);
                _logger.LogInformation($"Bot has joined a new guild {guild.Name} with id - {guild.Id}");
            };

            _client.LeftGuild += async guild => {
                await _dataStorage.UnregisterGuild(guild.Id);
                _logger.LogInformation($"Bot has left a guild {guild.Name} with id - {guild.Id}");
            };

            _client.UserJoined += async user => {
                _logger.LogInformation($"User {user.Username}@{user.Id} has joined the guild {user.Guild.Name}@{user.Guild.Id}");
                Guild guild = await _dataStorage.GetGuildAsync(user.Guild.Id);
                if (guild.WelcomeChannelId is not null) {
                    IChannel channel = await _client.GetChannelAsync(guild.WelcomeChannelId.Value);
                    if (channel is not SocketTextChannel textChannel) {
                        _logger.LogError("Attempt to send a new user message has failed " +
                                         "because registered channel was not a text channel");
                        return;
                    }

                    MessageProperties properties = _UIService.GenerateNewUserMessage(user.Guild.CurrentUser, user);
                    await textChannel.SendMessageAsync(
                        text: properties.Content.GetValueOrDefault(),
                        embed: properties.Embed.GetValueOrDefault(),
                        components: properties.Components.GetValueOrDefault(),
                        options: new RequestOptions {
                            RetryMode = RetryMode.Retry502 | RetryMode.RetryTimeouts
                        });
                }
            };

            _client.UserLeft += async (guild, user) => {
                _logger.LogInformation($"User {user.Username}@{user.Id} has left the guild {guild.Name}@{guild.Id}");
                Guild guildData = await _dataStorage.GetGuildAsync(guild.Id);
                if (guildData.RunawayChannelId is not null) {
                    IChannel channel = await _client.GetChannelAsync(guildData.RunawayChannelId.Value);
                    if (channel is not SocketTextChannel textChannel) {
                        _logger.LogError("Attempt to send a left user message has failed " +
                                         "because registered channel was not a text channel");
                        return;
                    }

                    MessageProperties properties = _UIService.GenerateLeftUserMessage(guild.CurrentUser, user);
                    await textChannel.SendMessageAsync(
                        text: properties.Content.GetValueOrDefault(),
                        embed: properties.Embed.GetValueOrDefault(),
                        components: properties.Components.GetValueOrDefault(),
                        options: new RequestOptions {
                            RetryMode = RetryMode.Retry502 | RetryMode.RetryTimeouts
                        });
                }
            };

            _client.UserVoiceStateUpdated += async (user, oldState, newState) => {
                if (user.Id == _client.CurrentUser.Id && oldState.VoiceChannel.Id != newState.VoiceChannel.Id) {
                    MusicPlayerMetadata metadata = _audioService.GetMusicPlayerMetadata(newState.VoiceChannel.Guild.Id);
                    metadata.VoiceChannel = newState.VoiceChannel;

                    if (metadata.AudioClient?.ConnectionState != ConnectionState.Disconnected) {
                        metadata.ReconnectRequested = true;
                        return;
                    }

                    await _audioService.PlayQueueAsync(newState.VoiceChannel.Guild.Id, force: true);
                }
            };

            _client.InteractionCreated += async interaction => {
                IResult result = await _interactions.ExecuteCommandAsync(
                    new SocketInteractionContext(_client, interaction), _serviceProvider);

                if (result.IsSuccess) {
                    return;
                }

                throw new InteractionCommandException(interaction, (InteractionCommandError)result.Error!, 
                    result.ErrorReason);
            };

            _client.ButtonExecuted += ClientOnButtonExecuted;
            
            
            _client.Ready += async () => {
                //await _interactions.RegisterCommandsGloballyAsync();
                foreach (Guild guild in await _dataStorage.GetMusicGuildsAsync()) {
                    await _interactions.RegisterCommandsToGuildAsync(guild.Id);
                    
                    SocketTextChannel musicChannel = await GetChannelAsync(guild.MusicChannelId!.Value);
                    _dataStorage.RegisterMusicChannelImpl(guild.Id, musicChannel);

                    _audioService.CreateMusicPlayerMetadata(guild.Id);
                    await _audioService.UpdateSongsQueueAsync(guild.Id, "Queue was cleared");
                }
                
                _logger.LogInformation("Bot is ready");
            };

            _interactions.AddTypeConverter<MusicProvider>(new SmartEnumTypeConverter<MusicProvider, BaseMusicProviderController>());
            await _interactions.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);

            await _client.LoginAsync(TokenType.Bot, _settings.DiscordBotToken);
            await _client.StartAsync();
            

            _logger.LogInformation("Discord service initialized");
        }


        public async Task EnsureBotIsReady(SocketInteraction interaction)
        {
            if (_client.ConnectionState != ConnectionState.Connected) {
                await interaction.ModifyOriginalResponseAsync(m => 
                    m.Content = "Bot is not ready to accept this command");
                throw new InteractionCommandException(interaction, InteractionCommandError.Unsuccessful,
                    $"Bot was not ready to handle the [{interaction.Id}] interaction");
            }
        }

        private async Task<SocketTextChannel> GetChannelAsync(ulong channelId, CancellationToken token = default)
        {
            RequestOptions options = new RequestOptions {CancelToken = token};
            IChannel channel = await _client.GetChannelAsync(channelId, options);
            if (channel is not SocketTextChannel textChannel) {
                throw new ArgumentException("Channel with this ID is not a text channel",
                    nameof(channelId));
            }
            
            return textChannel;
        }

        private async Task ClientOnButtonExecuted(SocketMessageComponent component)
        {
            _logger.LogInformation($"Button with ID [{component.Data.CustomId}] executed");
    
            string prefix = new string(component.Data.CustomId.TakeWhile(c => c != '-').ToArray());
            if (!_buttonHandlers.TryGetValue(prefix, out IButtonHandler? handler)) {
                throw new InvalidOperationException($"Button handler for the prefix {prefix} was not found");
            }
            
            SocketGuild guild = _client.GetGuild((ulong)component.GuildId!);

            await component.DeferAsync();
            ThreadPool.QueueUserWorkItem(
                    async (c) => 
                        await c.handler.OnButtonExecuted(c.component, c.guild), 
                    (handler, component, guild),
                    true);
        }
    }
}