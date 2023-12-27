namespace DungeonDiscordBot.InternalAPIs.SpotifyDown.Exceptions;

public class SpotifyDownRequestException : Exception
{
    public object? Response { get; }

    public SpotifyDownRequestException(Uri requestUri, string message):
        base($"The request to {requestUri.AbsoluteUri} was completed with an error: {message}") { }

    public SpotifyDownRequestException(Uri requestUri, string message, object response) :
        this(requestUri, message)
    {
        Response = response;
    }
}