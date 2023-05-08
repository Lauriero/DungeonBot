using System.Text.RegularExpressions;

using DungeonDiscordBot.Model;

using Newtonsoft.Json.Linq;

using VkNet.Model.Attachments;

using Yandex.Music.Api;
using Yandex.Music.Api.Common;
using Yandex.Music.Api.Models.Common;
using Yandex.Music.Api.Models.Track;

namespace DungeonDiscordBot.MusicProvidersControllers;

public class YandexMusicProviderController : BaseMusicProviderController
{
    private YandexMusicApi _api;
    private AuthStorage _apiAuth;
    
    public override string LinksDomainName => "music.yandex.ru";

    public YandexMusicProviderController()
    {
        _api = new YandexMusicApi();
        _apiAuth = new AuthStorage();
    }
    
    public override async Task InitializeAsync()
    {
        await _api.User.AuthorizeAsync(_apiAuth, "AQAAAABBD576AAG8Xn5K9oGDJU5sqRCu-rxq8p0");
    }

    public override async Task<IEnumerable<AudioQueueRecord>> GetAudiosFromLink(Uri link)
    {
        Regex albumRegex = new Regex(@".+/album/(\d+)");
        Regex trackRegex = new Regex(@".+/album/(\d+)/track/(\d+)");
        Regex artistRegex = new Regex(@".+/artist/(\d+)");  
        Regex userPlaylistRegex = new Regex(@".+/users/(\w+)/playlists/(\d+)");
        
        string url = link.AbsoluteUri;
        Match albumMatch = albumRegex.Match(url);
        Match trackMatch = trackRegex.Match(url);
        Match artistMatch = artistRegex.Match(url);
        Match userPlaylistMatch = userPlaylistRegex.Match(url);

        IEnumerable<YTrack> tracks;
        if (userPlaylistMatch.Success) {
            var playlist = await _api.Playlist.GetAsync(_apiAuth, userPlaylistMatch.Groups[1].Value,
                userPlaylistMatch.Groups[2].Value);
            tracks = playlist.Result.Tracks.Select(tc => tc.Track);
        }

        var album = await _api.Album.GetAsync(_apiAuth, "14734403");
        var fileLink = await _api.Track.GetFileLinkAsync(_apiAuth, album.Result.Volumes[0][0]);
        
        return Array.Empty<AudioQueueRecord>();
    }
}