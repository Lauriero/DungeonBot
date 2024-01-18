using VkNet.Abstractions;
using VkNet.AudioApi.Model;
using VkNet.AudioApi.Model.General;
using VkNet.Enums.Filters;
using VkNet.Model;

using Lyrics = VkNet.AudioApi.Model.Lyrics;

namespace VkNet.AudioApi;

public interface IVkAudioApi
{
    Task<string> AuthAsync(string login, string password, Func<string> twoFactorAuth);
    Task SetTokenAsync(string token);

    Task<Model.Group?> GetGroupByIdAsync(IEnumerable<string> groupIds, GroupsFields fields);
    
    Task<ResponseData> GetAudioCatalogAsync(string? url = null);
    Task<ResponseData> GetSectionAsync(string sectionId, string? startFrom = null);
    Task<ResponseData> GetBlockItemsAsync(string blockId);
    
    Task<List<Audio>> SearchAudioAsync(string query, string? context = null);
    Task<ResponseData> GetAudioSearchCatalogAsync(string? query = null, string? context = null);

    Task<ResponseData> GetAudioArtistAsync(string artistId);
    Task<ResponseData> GetAudioCuratorAsync(string curatorId, string url);
    Task<Playlist> GetPlaylistAsync(long albumId, string accessKey, long ownerId, int offset = 0, int count = 100, int needOwner = 1);
    Task AudioAddAsync(long audioId, long ownerId);
    Task AudioDeleteAsync(long audioId, long ownerId);
    Task<User?> GetCurrentUserAsync();
    Task<User?> GetUserAsync(long userId);
    Task<Owner?> OwnerAsync(long ownerId);
    Task AddPlaylistAsync(long playlistId, long ownerId, string accessKey);
    Task DeletePlaylistAsync(long playlistId, long ownerId);

    /// <summary>
    /// Fetches the array of audios by their ids.
    /// </summary>
    /// <param name="audios">List of one or multiple audio ids, separated with ","</param>
    /// <example>
    /// await GetAudiosByIdAsync(new [] {"371745461_456289486,-41489995_202246189"});
    /// </example>
    Task<List<Audio>> GetAudiosByIdAsync(IEnumerable<string> audios);
    
    /// <summary>
    /// Fetches all user audios or audios in the specified playlist.
    /// </summary>
    /// <param name="playlistId">Id of the playlist, tracks should be fetched from.</param>
    /// <param name="ownerId">Id of the owner of the playlist, or the user.</param>
    /// <param name="assessKey">Access key to the playlist.</param>
    /// <param name="offset">Specifies how many audios are needed to be skipped.</param>
    /// <param name="count">Specifies the maximum amount of audios that will be fetched.</param>
    Task<List<Audio>> AudioGetAsync(long? playlistId, long? ownerId, string? assessKey, long offset = 0, long count = 100);
    Task<ResponseData> ReplaceBlockAsync(string replaceId);
    Task StatsTrackEvents(List<TrackEvent> obj);
    Task FollowCurator(long curatorId);
    Task UnfollowCurator(long curatorId);
    Task FollowArtist(string artistId, string referenceId);
    Task UnfollowArtist(string artistId, string referenceId);
    Task<RestrictionPopupData> AudioGetRestrictionPopup(string trackCode, string audio);
    Task<ResponseData> GetPodcastsAsync(string? url = null);
    Task<ResponseData> SectionHomeAsync();
    Task<ResponseData> GetRecommendationsAudio(string audio);
    Task SetBroadcastAsync(Audio? audio);
    Task<List<Playlist>> GetPlaylistsAsync(long ownerId);
    Task AddToPlaylistAsync(Audio audio, long ownerId, long playlistId);
    Task<long> CreatePlaylistAsync(long ownerId, string title, string description, IEnumerable<Audio> tracks);
    Task SetPlaylistCoverAsync(long ownerId, long playlistId, string hash, string photo);
    Task<UploadPlaylistCoverServerResult> GetPlaylistCoverUploadServerAsync(long ownerId, long playlistId);
    Task<UploadPlaylistCoverResult> UploadPlaylistCoverAsync(string uploadUrl, string path);
    Task EditPlaylistAsync(long ownerId, int playlistId, string title, string description, List<Audio> tracks);
    Task<BoomToken?> GetBoomToken();
    Task FollowOwner(long ownerId);
    Task UnfollowOwner(long ownerId);
    Task<Lyrics> GetLyrics(string audioId);
}