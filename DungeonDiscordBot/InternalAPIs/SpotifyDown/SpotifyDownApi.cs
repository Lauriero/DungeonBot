using System.Net.Http.Headers;

using DungeonDiscordBot.InternalAPIs.SpotifyDown.Exceptions;
using DungeonDiscordBot.InternalAPIs.SpotifyDown.Model;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

namespace DungeonDiscordBot.InternalAPIs.SpotifyDown;

/// <inheritdoc />
public class SpotifyDownApi : ISpotifyDownApi
{
    private readonly HttpClient _invoker;
    private readonly ILogger<SpotifyDownApi> _logger;
    private static readonly IReadOnlyDictionary<string, string> _mandatoryHeaders = new Dictionary<string, string> {
        {"User-Agent", "Other"},
        {"referer", "https://spotifydown.com/"},
        {"origin", "https://spotifydown.com"}
    };
    
    public SpotifyDownApi(
        ILogger<SpotifyDownApi> logger,
        [FromKeyedServices(ISpotifyDownApi.HTTP_CLIENT_SERVICE_KEY)] HttpClient invoker)
    {
        _invoker = invoker;
        _logger = logger;
    }
    
    /// <inheritdoc />
    public async Task<string?> GetYoutubeIdAsync(string trackId, 
        CancellationToken token = default)
    {
        Uri targetUri = new Uri($"https://api.spotifydown.com/getId/{trackId}");
        GetIdResponse? response = await ExecuteBaseAsync<GetIdResponse>(targetUri, token);

        if (response is null) {
            _logger.LogError("Method 'https://api.spotifydown.com/getId/' returned null");
            throw new SpotifyDownRequestException(targetUri, "This method could not return null");
        }

        if (response.Success) {
            return response.Id;
        }

        if (response.Message == "No results found") {
            _logger.LogError("No YouTube video ID was found for a track with id {tId}", trackId);
            return null;
        }

        throw new SpotifyDownRequestException(targetUri, $"The message {response.Message} couldn't be handled",
            response);
    }

    private async Task<TResponse?> ExecuteBaseAsync<TResponse>(Uri requestUri, CancellationToken token = default)
        where TResponse : class
    {
        string responseString;
        using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri)) {
            HttpRequestHeaders headers = request.Headers;
            for (int i = 0; i < _mandatoryHeaders.Count; ++i) {
                (string key, string value) = _mandatoryHeaders.ElementAt(i);
                headers.TryAddWithoutValidation(key, value);
            }

            using (HttpResponseMessage response = await _invoker.SendAsync(request, HttpCompletionOption.ResponseContentRead, token)) {
                if (!response.IsSuccessStatusCode) {
                    _logger.LogError("The request to {uri} was completed with a status code {code}", 
                        requestUri.AbsoluteUri, response.StatusCode);
                }

                responseString = await response.Content.ReadAsStringAsync();
            }
        }

        _logger.LogDebug("Successfully requested {uri} with response: \n{response}", 
            requestUri.AbsoluteUri, responseString);
        try {
            TResponse? responseObject = JsonConvert.DeserializeObject<TResponse>(responseString);
            return responseObject;
        } catch (Exception e) {
            _logger.LogError(e, "Failed attempt to deserialize the response to {uri}", requestUri.AbsoluteUri);
            throw;
        }
    }

    public void Dispose()
    {
        _invoker.Dispose();
        GC.SuppressFinalize(this);
    }
}