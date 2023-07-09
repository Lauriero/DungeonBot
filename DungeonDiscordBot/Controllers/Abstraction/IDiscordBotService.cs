using Discord.WebSocket;

namespace DungeonDiscordBot.Controllers.Abstraction
{
    public interface IDiscordBotService : IRequireInitiationService
    {
        Task EnsureBotIsReady(SocketInteraction interaction);
        
        Task HandleInteractionException(Exception exception);

        Task<SocketTextChannel> GetChannelAsync(ulong channelId, CancellationToken token = default);
    }
}