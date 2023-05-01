using Discord.Interactions;
using Discord.WebSocket;

using Microsoft.VisualBasic;

namespace DungeonDiscordBot.Exceptions;

public class InteractionCommandException : Exception
{
    public InteractionCommandException(SocketInteraction interaction, InteractionCommandError error, string message): 
        base($"Error occured during execution of an interaction [{interaction.Id}]: {error} - {message}") { }
}