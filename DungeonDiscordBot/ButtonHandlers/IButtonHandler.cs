using System.Threading.Tasks;

using Discord.WebSocket;

namespace DungeonDiscordBot.ButtonHandlers;

public interface IButtonHandler
{ 
    /// <summary>
    /// Prefix in command ID that determines that the button
    /// should be handled by the current handler.
    /// </summary>
    string Prefix { get; }
    
    Task OnButtonExecuted(SocketMessageComponent component, SocketGuild guild);
}