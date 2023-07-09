using System.Collections.Concurrent;
using System.Reflection;

using Discord;
using Discord.Interactions;
using Discord.WebSocket;

using DungeonDiscordBot.ButtonHandlers;
using DungeonDiscordBot.Controllers.Abstraction;
using DungeonDiscordBot.Exceptions;
using DungeonDiscordBot.Model;
using DungeonDiscordBot.Model.Database;
using DungeonDiscordBot.MusicProvidersControllers;
using DungeonDiscordBot.TypeConverters;
using DungeonDiscordBot.Utilities;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DungeonDiscordBot.Controllers
{
    public class DiscordBotService : IDiscordBotService
    {
        public int InitializationPriority => 10;

        private readonly ILogger<IDiscordBotService> _logger; 
        private readonly AppSettings _settings;
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _interactions;
        private readonly ISettingsService _settingsService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDiscordAudioService _audioService;
        private readonly ConcurrentDictionary<string, IButtonHandler> _buttonHandlers;
        
        public DiscordBotService(IServiceProvider serviceProvider, ILogger<IDiscordBotService> logger, 
            DiscordSocketClient client, InteractionService interactions, IOptions<AppSettings> settings, 
            ISettingsService settingsService, IDiscordAudioService audioService)
        {
            _logger = logger;
            _client = client;
            _settings = settings.Value;
            _interactions = interactions;
            _settingsService = settingsService;
            _audioService = audioService;

            _serviceProvider = serviceProvider;
            _buttonHandlers = new ConcurrentDictionary<string, IButtonHandler>(
                _serviceProvider.GetAllServices<IButtonHandler>().Select(handler =>
                    new KeyValuePair<string, IButtonHandler>(handler.Prefix, handler)));
        }


        public async Task InitializeAsync()
        {
            _logger.LogInformation("Initializing Discord service...");

            _client.JoinedGuild += async guild => {
                await _settingsService.RegisterGuild(guild.Id, guild.Name);
                _logger.LogInformation($"Bot has joined a new guild {guild.Name} with id - {guild.Id}");
            };

            _client.LeftGuild += async guild => {
                await _settingsService.UnregisterGuild(guild.Id);
                _logger.LogInformation($"Bot has left a guild {guild.Name} with id - {guild.Id}");
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
                await _interactions.RegisterCommandsGloballyAsync();

                foreach (Guild guild in await _settingsService.GetMusicGuildsAsync()) {
                    _audioService.CreateMusicPlayerMetadata(guild.Id);
                    await _audioService.UpdateSongsQueueAsync(guild.Id);
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

        public async Task HandleInteractionException(Exception exception)
        {
            _logger.LogError(exception, "Interaction was executed with an exception");
        }

        public async Task<SocketTextChannel> GetChannelAsync(ulong channelId, CancellationToken token = default)
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