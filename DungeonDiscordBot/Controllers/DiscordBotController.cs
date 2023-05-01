using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reflection;
using System.Threading.Tasks;

using Discord;
using Discord.Interactions;
using Discord.WebSocket;

using DungeonDiscordBot.ButtonHandlers;
using DungeonDiscordBot.Controllers.Abstraction;
using DungeonDiscordBot.Exceptions;
using DungeonDiscordBot.Model;
using DungeonDiscordBot.MusicProvidersControllers;
using DungeonDiscordBot.TypeConverters;
using DungeonDiscordBot.Utilities;

using Serilog;

namespace DungeonDiscordBot.Controllers
{
    public class DiscordBotController : IDiscordBotController
    {
        private IServicesAggregator _aggregator = null!;
        private readonly ILogger _logger; 
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _interactions;
        private readonly Dictionary<string, IButtonHandler> _buttonHandlers = new();

        public DiscordBotController(ILogger logger, DiscordSocketClient client, InteractionService interactions)
        {
            _logger = logger;
            _interactions = interactions;
            _client = client;
        }
        
        public async Task Init(IServicesAggregator aggregator)
        {
            _aggregator = aggregator;
            
            _logger.Information("Initializing Discord service...");

            _client.InteractionCreated += async interaction => {
                IResult result = await _interactions.ExecuteCommandAsync(new SocketInteractionContext(_client, interaction),
                    _aggregator.ServiceProvider);;

                if (result.IsSuccess) {
                    return;
                }

                throw new InteractionCommandException(interaction, (InteractionCommandError)result.Error!, result.ErrorReason);
            };

            _client.ButtonExecuted += ClientOnButtonExecuted;

            _client.Ready += async () => {
                await _interactions.RegisterCommandsGloballyAsync();
                Console.WriteLine("Bot is ready.");
            };

            _interactions.AddTypeConverter<MusicProvider>(new SmartEnumTypeConverter<MusicProvider, BaseMusicProviderController>());

            SearchButtonHandlers(Assembly.GetEntryAssembly()!, _aggregator);
            await _interactions.AddModulesAsync(Assembly.GetEntryAssembly(), _aggregator.ServiceProvider);

            await _client.LoginAsync(TokenType.Bot, ApiCredentials.DISCORD_BOT_TOKEN);
            await _client.StartAsync();
            

            _logger.Information("Discord service initialized");
        }

        public Task HandleInteractionException(Exception exception)
        {
            return Task.Factory.StartNew(() => throw exception);
        }

        private async Task ClientOnButtonExecuted(SocketMessageComponent component)
        {
            string prefix = new string(component.Data.CustomId.TakeWhile(c => c != '-').ToArray());
            if (!_buttonHandlers.TryGetValue(prefix, out IButtonHandler? handler)) {
                throw new InvalidOperationException($"Button handler for the prefix {prefix} was not found");
            }            
            
            await handler!.OnButtonExecuted(component);
        }

        private void SearchButtonHandlers(Assembly assembly, IServicesAggregator aggregator)
        {
            foreach (Type handlerType in assembly.GetTypes().Where(t => typeof(IButtonHandler).IsAssignableFrom(t) && t.IsClass)) {
                bool emptyCtor = false;
                if (handlerType.GetConstructors().Any(c => {
                        var parameters = c.GetParameters();
                        emptyCtor = parameters.Length == 0;
                        return (parameters.Length == 1 && parameters[0].ParameterType == typeof(IServicesAggregator)) 
                               || emptyCtor;
                    })) {
                    
                    IButtonHandler? instance;
                    if (emptyCtor) {
                        instance = (IButtonHandler?)Activator.CreateInstance(handlerType);
                    } else {
                        instance = (IButtonHandler?)Activator.CreateInstance(handlerType, aggregator);
                    }
                    
                    if (instance == null) {
                        throw new InvalidOperationException("Unable to create instance of " + handlerType);
                    }
                    
                    _buttonHandlers.Add(instance.Prefix, instance);
                } else {
                    throw new InvalidOperationException(
                        $"{handlerType} doesn't have empty constructor or constructor that accepts service aggregator");
                }
            }
        }
    }
}