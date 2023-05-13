using System;

namespace DungeonDiscordBot.Exceptions;

/// <summary>
/// Is thrown on an exception that occurred while working with one of the music providers. 
/// </summary>
public class MusicProviderException : Exception
{
    public MusicProviderException(string message): base(message)
    {
        
    }
}