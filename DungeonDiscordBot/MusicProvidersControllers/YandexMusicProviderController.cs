using System.Text.RegularExpressions;

using DungeonDiscordBot.Model;
using DungeonDiscordBot.Model.MusicProviders;
using DungeonDiscordBot.Model.MusicProviders.Search;
using DungeonDiscordBot.Settings;

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
    public override string DisplayName => "YandexMusic";
    public override string LinksDomainName => "music.yandex.ru";
    public override string LogoEmojiId => "<:logo_yandex_music:1189750003141455922>";
    public override string LogoUri => "http://larc.tech/content/dungeon-bot/logo-yandex-music.png";
    public override string SupportedLinks =>
        "Use https://music.yandex.ru/album/{albumId}/track/{trackId} to retrieve a single track\n" +
        "Use https://music.yandex.ru/album/{albumId} to retrieve tracks from the album\n" +
        "Use https://music.yandex.ru/artist/{artistId} to retrieve all artist's tracks\n" +
        "Use https://music.yandex.ru/users/{username}/playlists/{playlistId} to retrieve tracks from the playlist";

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
        Regex userPlaylistRegex = new Regex(@"^.+/users/([-\w]+)/playlists/(\d+)([?/]?)(.*)$");
        
        string url = link.AbsoluteUri;
        Match albumMatch = albumRegex.Match(url);
        Match trackMatch = trackRegex.Match(url);
        Match artistMatch = artistRegex.Match(url);
        Match userPlaylistMatch = userPlaylistRegex.Match(url);

        IEnumerable<YTrack> tracks;
        string collectionName;
        // #TODO: Handle exception when unable to find playlist
        try {
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

                YResponse<YArtistBriefInfo> artistInfo =
                    await _api.Artist.GetAsync(_apiAuth, artistMatch.Groups[1].Value);
                collectionName = $"{artistInfo.Result.Artist.Name} - All tracks";
            } else {
                return MusicCollectionResponse.FromError(MusicProvider.Yandex, MusicResponseErrorType.LinkNotSupported,
                    $"Current provider can't handle urls like {url}");
            }
        } catch (YErrorResponse) {
            return MusicCollectionResponse.FromError(MusicProvider.Yandex, MusicResponseErrorType.NoAudioFound,
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
                provider:                 MusicProvider.Yandex,
                author:                   track.Artists.First().Name, 
                title:                    track.Title,
                duration:                 TimeSpan.FromMilliseconds(track.DurationMs),
                audioUriFactory:          () => _api.Track.GetFileLinkAsync(_apiAuth, track),
                audioThumbnailUriFactory: () => track.CoverUri is null 
                    ? Task.FromResult<string?>(null) 
                    : Task.FromResult<string?>($"https://{track.CoverUri.Replace("%%", "200x200")}"),
                publicUrl:                $"https://music.yandex.ru/album/{track.Albums[0].Id}/track/{track.Id}"));
        }

        return MusicCollectionResponse.FromSuccess(MusicProvider.Yandex, collectionName, records);
    }

    public override async Task<MusicSearchResult> SearchAsync(string query, MusicCollectionType targetCollectionType, int? count = null)
    {
        switch (targetCollectionType) {
            case MusicCollectionType.Track:
                YResponse<YSearch> trackSearchResult = await _api.Search.SearchAsync(_apiAuth, query, YSearchType.Track, 
                    pageSize: count ?? MaxSearchResultsCount);
                return new MusicSearchResult(
                    provider: MusicProvider.Yandex,
                    entities:   trackSearchResult.Result.Tracks.Results
                        .Take(MaxSearchResultsCount)
                        .Select(t => {  
                            string artists = string.Join(", ", t.Artists.Select(a => a.Name));
                            return new SearchResultEntity(
                                name: $"{artists} - {t.Title}",
                                link: $"https://music.yandex.ru/album/{t.Albums[0].Id}/track/{t.Id}");
                        }));

            case MusicCollectionType.Artist:
                YResponse<YSearch> artistSearchResult = await _api.Search.SearchAsync(_apiAuth, query, YSearchType.Artist, 
                    pageSize: MaxSearchResultsCount);
                
                if (artistSearchResult.Result.Artists is null) {
                    return new MusicSearchResult(MusicProvider.Yandex, Array.Empty<SearchResultEntity>());
                }
                
                return new MusicSearchResult(
                    provider: MusicProvider.Yandex,
                    entities:   artistSearchResult.Result.Artists.Results
                        .Take(MaxSearchResultsCount)
                        .Select(a => new SearchResultEntity(
                            name: a.Name,
                            link: $"https://music.yandex.ru/artist/{a.Id}")));

            case MusicCollectionType.Album:
                YResponse<YSearch> albumSearchResult = await _api.Search.SearchAsync(_apiAuth, query, YSearchType.Album,
                    pageSize: MaxSearchResultsCount);

                if (albumSearchResult.Result.Albums is null) {
                    return new MusicSearchResult(MusicProvider.Yandex, Array.Empty<SearchResultEntity>());
                }
                
                return new MusicSearchResult(
                    provider: MusicProvider.Yandex,
                    entities:   albumSearchResult.Result.Albums.Results
                        .Take(MaxSearchResultsCount)
                        .Select(a => {  
                            string artists = string.Join(", ", a.Artists.Select(artist => artist.Name));
                            return new SearchResultEntity(
                                name: $"{artists} - {a.Title}",
                                link: $"https://music.yandex.ru/album/{a.Id}");
                        }));

            case MusicCollectionType.Playlist:
                YResponse<YSearch> playlistSearchResult = await _api.Search.SearchAsync(_apiAuth, query, YSearchType.Playlist, 
                    pageSize: MaxSearchResultsCount);
                
                if (playlistSearchResult.Result.Playlists is null) {
                    return new MusicSearchResult(MusicProvider.Yandex, Array.Empty<SearchResultEntity>());
                }
                
                return new MusicSearchResult(
                    provider: MusicProvider.Yandex,
                    entities:   playlistSearchResult.Result.Playlists.Results
                        .Take(MaxSearchResultsCount)
                        .Select(p => new SearchResultEntity(
                            name: $"{p.Owner.Name} - {p.Title}",
                            link: $"https://music.yandex.ru/users/{p.Owner.Login}/playlists/{p.Kind}")));
            default:
                throw new ArgumentOutOfRangeException(nameof(targetCollectionType), targetCollectionType, null);
        }
    }
}