using Newtonsoft.Json;

namespace VkNet.AudioApi.Model
{
    public class StopTrackEvent: TrackEvent
    {
        [JsonProperty("audio_id")]
        public string AudioId { get; set; } = null!;

        [JsonProperty("start_time")]
        public string StartTime { get; set; } = null!;

        [JsonProperty("shuffle")]
        public string Shuffle { get; set; } = null!;

        [JsonProperty("reason")]
        public string Reason { get; set; } = null!;

        [JsonProperty("playback_started_at")]
        public string PlaybackStartedAt { get; set; } = null!;

        [JsonProperty("track_code")]
        public string TrackCode { get; set; } = null!;

        [JsonProperty("repeat")]
        public string Repeat { get; set; } = null!;

        [JsonProperty("state")]
        public string State { get; set; } = null!;

        [JsonProperty("source")]
        public string Source { get; set; } = null!;

        [JsonProperty("duration")]
        public string Duration { get; set; } = null!;

        [JsonProperty("playlist_id")]
        public string PlaylistId { get; set; } = null!;

        [JsonProperty("streaming_type")]
        public string StreamingType { get; set; } = null!;
    }
}
