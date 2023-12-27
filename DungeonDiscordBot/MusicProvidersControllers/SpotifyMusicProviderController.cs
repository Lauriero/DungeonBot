using System.Text.RegularExpressions;

using DungeonDiscordBot.InternalAPIs.SpotifyDown;
using DungeonDiscordBot.Model;
using DungeonDiscordBot.Model.MusicProviders;
using DungeonDiscordBot.Model.MusicProviders.Search;
using DungeonDiscordBot.Settings;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SpotifyAPI.Web;

using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace DungeonDiscordBot.MusicProvidersControllers;

public class SpotifyMusicProviderController : BaseMusicProviderController
{
    public override string LinksDomainName => "spotify.com";
    public override string LogoUri => "http://larc.tech/content/dungeon-bot/logo-spotify.png";

    private readonly SpotifyClient _spotifyApi;
    private readonly YoutubeClient _youtubeApi;
    private readonly ISpotifyDownApi _spotifyDownApi;
    private readonly ILogger<SpotifyMusicProviderController> _logger;

    public SpotifyMusicProviderController(ILogger<SpotifyMusicProviderController> logger, ISpotifyDownApi spotifyDownApi, 
        IOptions<AppSettings> options)
    {
        _logger = logger;
        _spotifyDownApi = spotifyDownApi;
        _youtubeApi = new YoutubeClient();
        
        AppSettings settings = options.Value;
        _spotifyApi = new SpotifyClient(SpotifyClientConfig
            .CreateDefault()
            .WithAuthenticator(new ClientCredentialsAuthenticator(settings.SpotifyClientId, settings.SpotifyClientSecret)));
    }
    
    public override async Task InitializeAsync()
    {
        _logger.LogInformation("Initializing Spotify music provider...");

        FullTrack track = await _spotifyApi.Tracks.Get("6XcoiOYiNbIxzpt8WRxq8Z");
        if (track.Name != "Du hast") {
            _logger.LogError("Spotify API check has failed");
            throw new Exception("Spotify API check has failed");
        }
        
        _logger.LogInformation("Spotify music provider initialized");
    }

    public override async Task<MusicCollectionResponse> GetAudiosFromLinkAsync(Uri link, int count)
    {
        string url = link.AbsoluteUri;
        Regex songRegex = new Regex(@".+track/([^?]*)\??(.*)");

        Match songMatch = songRegex.Match(url);
        
        string collectionName;
        List<FullTrack> tracks = new List<FullTrack>();
        if (songMatch.Success) {
            string trackId = songMatch.Groups[1].Value;
            
            FullTrack track = await _spotifyApi.Tracks.Get(trackId);
            collectionName = $"{GetTrackArtists(track)} - {track.Name}";
            tracks.Add(track);
        } else {
            return MusicCollectionResponse.FromError(MusicProvider.Spotify, MusicResponseErrorType.LinkNotSupported, 
                $"Current provider can't handle urls like {url}");
        }

        if (tracks.Count == 0) {
            return MusicCollectionResponse.FromError(MusicProvider.Spotify, MusicResponseErrorType.NoAudioFound, 
                "There's nothing in the requested playlist or album");
        }
        
        return MusicCollectionResponse.FromSuccess(
            provider: MusicProvider.Spotify, 
            name: collectionName,
            audios: tracks.Select(t => new AudioQueueRecord(
                provider: MusicProvider.Spotify, 
                author: GetTrackArtists(t),
                title: t.Name,
                audioUriFactory: async () => await GetTrackSource(t),
                audioThumbnailUriFactory: async () => t.Album.Images.FirstOrDefault()?.Url,
                duration: TimeSpan.FromMilliseconds(t.DurationMs),
                publicUrl: $"https://open.spotify.com/track/{t.Id}")));
    }

    public override async Task<MusicSearchResult> SearchAsync(string query, MusicCollectionType targetCollectionType, int? count = null)
    { 
        SearchResponse response = await _spotifyApi.Search.Item(new SearchRequest(SearchRequest.Types.Track, query));
        if (response.Tracks.Items is null) {
            return new MusicSearchResult(MusicProvider.Spotify, Array.Empty<SearchResultEntity>());
        }
        
        return new MusicSearchResult(
            provider: MusicProvider.Spotify, 
            entities: response.Tracks.Items
                .Select(t => {
                    string artists = string.Join(", ", t.Artists.Select(a => a.Name));
                    return new SearchResultEntity(
                        name: $"{artists} - {t.Name}",
                        link: $"https://open.spotify.com/track/{t.Id}");
                }));
    }

    private async Task<string> GetTrackSource(FullTrack track)
    {
        string? youtubeId = await _spotifyDownApi.GetYoutubeIdAsync(track.Id);
        if (youtubeId is null) {
            throw new ArgumentException("Corresponding YouTube video ID was not found", nameof(track));
        }

        StreamManifest manifest = await _youtubeApi.Videos.Streams
            .GetManifestAsync($"https://youtube.com/watch?v={youtubeId}");
        return manifest.GetAudioOnlyStreams().GetWithHighestBitrate().Url;
    }
    
    [Pure]
    private static string GetTrackArtists(FullTrack track)
    {
        return string.Join(", ", track.Artists.Select(a => a.Name));
    }
}