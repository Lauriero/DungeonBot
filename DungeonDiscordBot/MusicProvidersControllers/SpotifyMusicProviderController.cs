using System.Text.RegularExpressions;

using DungeonDiscordBot.Model;
using DungeonDiscordBot.Model.MusicProviders;
using DungeonDiscordBot.Model.MusicProviders.Records;
using DungeonDiscordBot.Model.MusicProviders.Search;
using DungeonDiscordBot.Settings;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SpotifyAPI.Web;

using YoutubeExplode;

namespace DungeonDiscordBot.MusicProvidersControllers;

public class SpotifyMusicProviderController : BaseMusicProviderController
{
    public override string DisplayName => "Spotify";
    public override string LinksDomainName => "open.spotify.com";
    public override string LogoEmojiId => "<:logo_spotify:1189750897711001631>";
    public override string LogoUri => "http://larc.tech/content/dungeon-bot/logo-spotify.png";

    public override string SupportedLinks =>
        "Use https://open.spotify.com/track/{trackId} to retrieve a single track\n" +
        "Use https://open.spotify.com/album/{albumId} to retrieve tracks from the album\n" +
        "Use https://open.spotify.com/artist/{artistId} to retrieve top tracks of the artist\n" +
        "Use https://open.spotify.com/playlist/{playlistId} to retrieve tracks from the playlist";

    private readonly SpotifyClient _spotifyApi;
    private readonly YoutubeClient _youtubeApi;
    private readonly ILogger<SpotifyMusicProviderController> _logger;

    public SpotifyMusicProviderController(ILogger<SpotifyMusicProviderController> logger,
        IOptions<AppSettings> options)
    {
        _logger = logger;
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
        
        List<FullTrack> tracks = new List<FullTrack>();
        MusicCollectionMetadata metadata = new MusicCollectionMetadata {PublicUrl = link.AbsoluteUri};
        try {
            if (songMatch.Success) {
                string trackId = songMatch.Groups[1].Value;

                FullTrack track = await _spotifyApi.Tracks.Get(trackId);
                metadata.Name = $"{GetTrackArtists(track)} - {track.Name}";
                metadata.Type = MusicCollectionType.Track;
                
                tracks.Add(track);
            } else if (albumMatch.Success) {
                string albumId = albumMatch.Groups[1].Value;

                FullAlbum album = await _spotifyApi.Albums.Get(albumId);
                TracksResponse albumTracks = await _spotifyApi.Tracks.GetSeveral(
                    new TracksRequest(album.Tracks.Items!.Select(t => t.Id).ToList()));

                string artists = string.Join(", ", album.Artists.Select(a => a.Name));
                metadata.Name = $"{artists} - {album.Name}";
                metadata.Type = MusicCollectionType.Album;
                
                tracks.AddRange(albumTracks.Tracks);
            } else if (playlistMatch.Success) {
                string playlistId = playlistMatch.Groups[1].Value;

                FullPlaylist playlist = await _spotifyApi.Playlists.Get(playlistId);
                foreach (PlaylistTrack<IPlayableItem> playlistTrack in playlist.Tracks!.Items!) {
                    if (playlistTrack.Track is FullTrack track) {
                        tracks.Add(track);
                    }
                }

                metadata.Name = $"{playlist.Owner!.DisplayName} - {playlist.Name}";
                metadata.Type = MusicCollectionType.Playlist;
            } else if (artistMatch.Success) {
                string artistId = artistMatch.Groups[1].Value;

                FullArtist artist = await _spotifyApi.Artists.Get(artistId);
                ArtistsTopTracksResponse topTracksResponse =
                    await _spotifyApi.Artists.GetTopTracks(artistId, new ArtistsTopTracksRequest("DE"));
                
                metadata.Name = $"{artist.Name} - Popular";
                metadata.Type = MusicCollectionType.Artist;
                tracks.AddRange(topTracksResponse.Tracks);
            } else {
                return MusicCollectionResponse.FromError(MusicProvider.Spotify, MusicResponseErrorType.LinkNotSupported,
                    $"Current provider can't handle urls like {url}");
            }
        } catch (APIException e) {
            return MusicCollectionResponse.FromError(MusicProvider.Spotify, MusicResponseErrorType.NoAudioFound, 
                $"Spotify API exception: {e.Message}");
        }

        if (tracks.Count == 0) {
            return MusicCollectionResponse.FromError(MusicProvider.Spotify, MusicResponseErrorType.NoAudioFound, 
                "There's nothing in the requested playlist or album");
        }
        
        return MusicCollectionResponse.FromSuccess(
            provider: MusicProvider.Spotify, 
            metadata: metadata,
            audios: tracks.Select(t => (AudioQueueRecord)new SpotifyAudioRecord(
                _youtubeApi,
                metadata:                 metadata,
                author:                   GetTrackArtists(t),
                title:                    t.Name,
                audioThumbnailUriFactory: async () => t.Album.Images.FirstOrDefault()?.Url,
                duration:                 TimeSpan.FromMilliseconds(t.DurationMs),
                publicUrl:                $"https://open.spotify.com/track/{t.Id}")).ToList());
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

    [Pure]
    private static string GetTrackArtists(FullTrack track)
    {
        return string.Join(", ", track.Artists.Select(a => a.Name));
    }
}