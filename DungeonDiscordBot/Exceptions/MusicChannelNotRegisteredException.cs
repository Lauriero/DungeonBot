namespace DungeonDiscordBot.Exceptions;

public class MusicChannelNotRegisteredException : Exception
{
    public MusicChannelNotRegisteredException()
        : base("Music channel was not registered before calling method that requires it")
    {
        
    }
}