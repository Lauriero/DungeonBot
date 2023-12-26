using Newtonsoft.Json;

using VkNet.Model;

namespace VkNet.AudioApi.Model.General
{
    public class ResponseVk
    {
        [JsonProperty("response")]
        public ResponseData Response { get; set; } = null!;

        [JsonProperty("error")]
        public ErrorVk Error { get; set; } = null!;
    }

    public class ResponseData
    {
        [JsonProperty("section")]
        public Section Section { get; set; } = null!;

        [JsonProperty("catalog")]
        public Catalog Catalog { get; set; } = null!;

        [JsonProperty("block")]
        public Block Block { get; set; } = null!;

        [JsonProperty("catalog_banners")]
        public List<CatalogBanner> CatalogBanners { get; set; } = null!;

        [JsonProperty("audios")]
        public List<Audio>? Audios { get; set; }

        [JsonProperty("playlists")]
        public List<Playlist> Playlists { get; set; } = null!;

        [JsonProperty("playlist")]
        public Playlist Playlist { get; set; } = null!;

        [JsonProperty("links")]
        public List<Link> Links { get; set; } = null!;

        [JsonProperty("artists")]
        public List<Artist> Artists { get; set; } = null!;

        [JsonProperty("suggestions")]
        public List<Suggestion> Suggestions { get; set; } = null!;

        [JsonProperty("curators")]
        public List<Curator> Curators { get; set; } = null!;

        [JsonProperty("groups")]
        public List<Group> Groups { get; set; } = null!;

        [JsonProperty("texts")]
        public List<Text> Texts { get; set; } = null!;

        [JsonProperty("items")]
        public List<Audio> Items { get; set; } = null!;

        [JsonProperty("replacements")]
        public Replacements Replacements { get; set; } = null!;

        [JsonProperty("profiles")]
        public List<User> Profiles { get; set; } = null!;

        [JsonProperty("longreads")]
        public List<Longread> Longreads { get; set; } = null!;

        [JsonProperty("podcast_episodes")]
        public List<PodcastEpisode> PodcastEpisodes { get; set; } = null!;

        [JsonProperty("podcast_slider_items")]
        public List<PodcastSliderItem> PodcastSliderItems { get; set; } = null!;

        [JsonProperty("recommended_playlists")]
        public List<RecommendedPlaylist> RecommendedPlaylists { get; set; } = null!;

        [JsonProperty("videos")]
        public List<Video> Videos { get; set; } = null!;

        [JsonProperty("artist_videos")]
        public List<Video> ArtistVideos { get; set; } = null!;

        [JsonProperty("placeholders")]
        public List<Placeholder> Placeholders { get; set; } = null!;

        [JsonProperty("music_owners")]
        public List<MusicOwner> MusicOwners { get; set; } = null!;

        [JsonProperty("audio_followings_update_info")]
        public List<AudioFollowingsUpdateInfo> FollowingsUpdateInfos { get; set; } = null!;
    }
}
