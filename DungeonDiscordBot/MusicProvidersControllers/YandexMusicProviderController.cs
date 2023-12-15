using System.Text.RegularExpressions;

using DungeonDiscordBot.Model;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Yandex.Music.Api;
using Yandex.Music.Api.Common;
using Yandex.Music.Api.Models.Artist;
using Yandex.Music.Api.Models.Common;
using Yandex.Music.Api.Models.Search;
using Yandex.Music.Api.Models.Search.Track;
using Yandex.Music.Api.Models.Track;

namespace DungeonDiscordBot.MusicProvidersControllers;

public class YandexMusicProviderController : BaseMusicProviderController
{
    public override string LinksDomainName => "music.yandex.ru";
    
    private readonly YandexMusicApi _api;
    private readonly AuthStorage _apiAuth;
    private readonly AppSettings _settings;
    private readonly ILogger<YandexMusicProviderController> _logger;

    public YandexMusicProviderController(IOptions<AppSettings> appSettings, 
        ILogger<YandexMusicProviderController> logger)
    {
        _logger = logger;
        _api = new YandexMusicApi();
        _apiAuth = new AuthStorage();
        _settings = appSettings.Value;
    }
    
    public override async Task InitializeAsync()
    {
        _logger.LogInformation("Initializing YAMusic provider...");
        await _api.User.AuthorizeAsync(_apiAuth, _settings.YMToken);
        _logger.LogInformation("YAMusic provider initialized");
    }

    public override async Task<MusicCollectionResponse> GetAudiosFromLinkAsync(Uri link, int count)
    {
        Regex albumRegex = new Regex(@"^.+/album/(\d+)([?/]?)(.*)$");
        Regex trackRegex = new Regex(@"^.+/album/(\d+)/track/(\d+)([?/]?)(.*)$");
        Regex artistRegex = new Regex(@"^.+/artist/(\d+)([?/]?)(.*)$");  
        Regex userPlaylistRegex = new Regex(@"^.+/users/(\w+)/playlists/(\d+)([?/]?)(.*)$");
        
        string url = link.AbsoluteUri;
        Match albumMatch = albumRegex.Match(url);
        Match trackMatch = trackRegex.Match(url);
        Match artistMatch = artistRegex.Match(url);
        Match userPlaylistMatch = userPlaylistRegex.Match(url);

        IEnumerable<YTrack> tracks;
        string collectionName;
        // #TODO: Handle exception when unable to find playlist
        if (userPlaylistMatch.Success) {
            var playlist = await _api.Playlist.GetAsync(_apiAuth, userPlaylistMatch.Groups[1].Value,
                userPlaylistMatch.Groups[2].Value);

            tracks = playlist.Result.Tracks.Select(tc => tc.Track);
            collectionName = $"{playlist.Result.Owner.Name} - {playlist.Result.Title}";
        } else if (trackMatch.Success) {
            var track = await _api.Track.GetAsync(_apiAuth, 
                $"{trackMatch.Groups[2].Value}:{trackMatch.Groups[1].Value}");
            
            tracks = track.Result;
            if (!tracks.Any()) {
                return MusicCollectionResponse.FromError(MusicProvider.VK, MusicResponseErrorType.NoAudioFound, 
                    $"There's no audio found on {url}");
            }

            YTrack firstTrack = tracks.First();
            string artists = string.Join(", ", firstTrack.Artists.Select(a => a.Name));
            collectionName = $"{artists} - {firstTrack.Title}";
        } else if (albumMatch.Success) {
            var album = await _api.Album.GetAsync(_apiAuth, albumMatch.Groups[1].Value);
            tracks = album.Result.Volumes.SelectMany(t => t);

            string artists = string.Join(", ", album.Result.Artists.Select(a => a.Name));
            collectionName = $"{artists} - {album.Result.Title}";
        } else if (artistMatch.Success) {
            var artist = await _api.Artist.GetAllTracksAsync(_apiAuth, 
                artistMatch.Groups[1].Value);
            
            tracks = artist.Result.Tracks;

            YResponse<YArtistBriefInfo> artistInfo = await _api.Artist.GetAsync(_apiAuth, artistMatch.Groups[1].Value);
            collectionName = $"{artistInfo.Result.Artist.Name} - All tracks";
        } else {
            return MusicCollectionResponse.FromError(MusicProvider.Yandex, MusicResponseErrorType.LinkNotSupported, 
                $"Current provider can't handle urls like {url}");
        }
        
        int toAddCount = tracks.Count();
        if (count > -1) {
            toAddCount = count;
        }

        IList<AudioQueueRecord> records = new List<AudioQueueRecord>();
        for (int i = 0; i < toAddCount; ++i) {
            YTrack track = tracks.ElementAt(i);
            if (!track.Available) {
                continue;
            }
            
            records.Add(new AudioQueueRecord(
                author:          track.Artists.First().Name, 
                title:           track.Title,
                duration:        TimeSpan.FromMilliseconds(track.DurationMs),
                audioUriFactory: () => _api.Track.GetFileLinkAsync(_apiAuth, track),
                audioThumbnailUriFactory: () => track.CoverUri is null 
                    ? Task.FromResult<string?>(null) 
                    : Task.FromResult<string?>($"https://{track.CoverUri.Replace("%%", "200x200")}")));
        }

        return MusicCollectionResponse.FromSuccess(MusicProvider.Yandex, collectionName, records);
    }

    public override async Task<MusicCollectionResponse> GetAudioFromSearchQueryAsync(string query)
    {
        YResponse<YSearch> searchResult = await _api.Search.TrackAsync(_apiAuth, query);
        List<YSearchTrackModel> tracks = searchResult.Result.Tracks.Results;
        if (tracks.Count == 0) {
            return MusicCollectionResponse.FromError(MusicProvider.Yandex, MusicResponseErrorType.NoAudioFound, 
                    "There's no audio that match the search query");
        }

        YTrack track = tracks.First();
        string artists = string.Join(", ", track.Artists.Select(a => a.Name));
        return MusicCollectionResponse.FromSuccess(MusicProvider.Yandex, 
            name: $"{artists} - {track.Title}",
            audios: new [] {
                new AudioQueueRecord(
                    author:          track.Artists.First().Name, 
                    title:           track.Title,
                    duration:        TimeSpan.FromMilliseconds(track.DurationMs),
                    audioUriFactory: () => _api.Track.GetFileLinkAsync(_apiAuth, track),
                    audioThumbnailUriFactory: () => track.CoverUri is null 
                        ? Task.FromResult<string?>(null) 
                        : Task.FromResult<string?>($"https://{track.CoverUri.Replace("%%", "200x200")}"))
            }
        );
    }
}