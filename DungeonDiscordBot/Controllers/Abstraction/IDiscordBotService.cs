using Discord.WebSocket;

namespace DungeonDiscordBot.Controllers.Abstraction
{
    public interface IDiscordBotService : IRequireInitiationService
    {
        Task EnsureBotIsReady(SocketInteraction interaction);
        
        Task HandleInteractionException(Exception exception);
    }
}