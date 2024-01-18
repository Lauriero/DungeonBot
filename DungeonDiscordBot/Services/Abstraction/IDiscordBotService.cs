using Discord.WebSocket;

namespace DungeonDiscordBot.Services.Abstraction
{
    public interface IDiscordBotService : IRequireInitiationService
    {
        Task EnsureBotIsReady(SocketInteraction interaction);
    }
}