using Newtonsoft.Json;

namespace VkNet.AudioApi.Model
{
    public class PodcastEpisode
    {
        [JsonProperty("artist")]
        public string Artist { get; set; } = null!;

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("owner_id")]
        public int OwnerId { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; } = null!;

        [JsonProperty("duration")]
        public int Duration { get; set; }

        [JsonProperty("is_explicit")]
        public bool IsExplicit { get; set; }

        [JsonProperty("is_focus_track")]
        public bool IsFocusTrack { get; set; }

        [JsonProperty("is_licensed")]
        public bool IsLicensed { get; set; }

        [JsonProperty("track_code")]
        public string TrackCode { get; set; } = null!;

        [JsonProperty("url")]
        public string Url { get; set; } = null!;

        [JsonProperty("date")]
        public int Date { get; set; }

        [JsonProperty("no_search")]
        public int NoSearch { get; set; }

        [JsonProperty("podcast_info")]
        public PodcastInfo PodcastInfo { get; set; } = null!;

        [JsonProperty("short_videos_allowed")]
        public bool ShortVideosAllowed { get; set; }

        [JsonProperty("stories_allowed")]
        public bool StoriesAllowed { get; set; }

        [JsonProperty("stories_cover_allowed")]
        public bool StoriesCoverAllowed { get; set; }
    }
}
