using System.Text.RegularExpressions;

using DungeonDiscordBot.InternalAPIs.SpotifyDown;
using DungeonDiscordBot.Model;
using DungeonDiscordBot.Model.MusicProviders;
using DungeonDiscordBot.Model.MusicProviders.Search;
using DungeonDiscordBot.Settings;
using DungeonDiscordBot.Utilities;

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
        Regex albumRegex = new Regex(@".+album/([^?]*)\??(.*)");
        Regex playlistRegex = new Regex(@".+playlist/([^?]*)\??(.*)");
        Regex artistRegex = new Regex(@".+artist/([^?]*)\??(.*)");

        Match songMatch = songRegex.Match(url);
        Match albumMatch = albumRegex.Match(url);
        Match playlistMatch = playlistRegex.Match(url);
        Match artistMatch = artistRegex.Match(url);
        
        string collectionName;
        List<FullTrack> tracks = new List<FullTrack>();
        if (songMatch.Success) {
            string trackId = songMatch.Groups[1].Value;

            FullTrack track = await _spotifyApi.Tracks.Get(trackId);
            collectionName = $"{GetTrackArtists(track)} - {track.Name}";
            tracks.Add(track);
        } else if (albumMatch.Success) {
            string albumId = albumMatch.Groups[1].Value;
            
            FullAlbum album = await _spotifyApi.Albums.Get(albumId);
            TracksResponse albumTracks = await _spotifyApi.Tracks.GetSeveral(
                new TracksRequest(album.Tracks.Items!.Select(t => t.Id).ToList()));
            
            string artists = string.Join(", ", album.Artists.Select(a => a.Name));
            collectionName = $"{artists} - {album.Name}";
            tracks.AddRange(albumTracks.Tracks);
        } else if (playlistMatch.Success) {
            string playlistId = playlistMatch.Groups[1].Value;

            FullPlaylist playlist = await _spotifyApi.Playlists.Get(playlistId);
            foreach (PlaylistTrack<IPlayableItem> playlistTrack in playlist.Tracks!.Items!) {
                if (playlistTrack.Track is FullTrack track) {
                    tracks.Add(track);
                }
            }

            collectionName = $"{playlist.Owner!.DisplayName} - {playlist.Name}";
        } else if (artistMatch.Success) {
            string artistId = artistMatch.Groups[1].Value;
            
            FullArtist artist = await _spotifyApi.Artists.Get(artistId);
            ArtistsTopTracksResponse topTracksResponse = await _spotifyApi.Artists.GetTopTracks(artistId, new ArtistsTopTracksRequest("DE"));
            
            collectionName = $"{artist.Name} - Popular";
            tracks.AddRange(topTracksResponse.Tracks);
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
        List<SearchResultEntity> results = new List<SearchResultEntity>();
        switch (targetCollectionType) {
            case MusicCollectionType.Track:
                SearchResponse tracksResponse = await _spotifyApi.Search.Item(new SearchRequest(SearchRequest.Types.Track, query));
                if (tracksResponse.Tracks.Items is null) {
                    break;
                }
                
                results.AddRange(tracksResponse.Tracks.Items
                    .Select(t => {
                        string artists = string.Join(", ", t.Artists.Select(a => a.Name));
                        return new SearchResultEntity(
                            name: $"{artists} - {t.Name}",
                            link: $"https://open.spotify.com/track/{t.Id}");
                    }));
                break;

            
            case MusicCollectionType.Artist:
                SearchResponse artistsResponse = await _spotifyApi.Search.Item(new SearchRequest(SearchRequest.Types.Artist, query));
                if (artistsResponse.Artists.Items is null) {
                    break;
                }
                
                results.AddRange(artistsResponse.Artists.Items
                    .Select(a => new SearchResultEntity(
                        name: a.Name,
                        link: $"https://open.spotify.com/artist/{a.Id}")));
                break;
                
                
            case MusicCollectionType.Album:
                SearchResponse albumsResponse = await _spotifyApi.Search.Item(new SearchRequest(SearchRequest.Types.Album, query));
                if (albumsResponse.Albums.Items is null) {
                    break;
                }
                
                results.AddRange(albumsResponse.Albums.Items
                    .Select(a => {
                        string artists = string.Join(", ", a.Artists.Select(a => a.Name));
                        return new SearchResultEntity(
                            name: $"{artists} - {a.Name}",
                            link: $"https://open.spotify.com/album/{a.Id}");
                    }));
                break;

            
            case MusicCollectionType.Playlist:
                SearchResponse playlistResponse = await _spotifyApi.Search.Item(new SearchRequest(SearchRequest.Types.Playlist, query));
                if (playlistResponse.Playlists.Items is null) {
                    break;
                }
                
                results.AddRange(playlistResponse.Playlists.Items
                    .Select(p => new SearchResultEntity(
                        name: $"{p.Owner!.DisplayName} - {p.Name}",
                        link: $"https://open.spotify.com/playlist/{p.Id}")));
                break;
            
                
            default:
                throw new ArgumentOutOfRangeException(nameof(targetCollectionType), targetCollectionType, null);
        }

        return new MusicSearchResult(MusicProvider.Spotify, results);
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