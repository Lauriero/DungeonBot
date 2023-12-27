namespace DungeonDiscordBot.InternalAPIs.SpotifyDown;

/// <summary>
/// Client to work with the http://spotifydown.com/ API methods.
/// </summary>
public interface ISpotifyDownApi : IDisposable
{
    /// <summary>
    /// The service key that is associated with a <see cref="HttpClient"/> entity,
    /// that will be resolved and used by this client to send HTTP requests.
    /// </summary>
    /// <remarks>
    /// <see cref="HttpClient"/> should be registered as transient service in order to dispose this service safely.
    /// </remarks>
    public const string HTTP_CLIENT_SERVICE_KEY = "spotify-http-client";

    /// <summary>
    /// Gets the best match YouTube ID of the spotify track
    /// using <see cref="!:https://spotifydown.com">spotifydown.com</see> service.
    /// </summary>
    /// <param name="trackId"></param>
    /// <param name="token"></param>
    public Task<string?> GetYoutubeIdAsync(string trackId, CancellationToken token = default);
}