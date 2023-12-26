using Newtonsoft.Json;

namespace VkNet.AudioApi.Model
{
    public class PlayPlaylistTrackEvent : TrackEvent
    {
        [JsonProperty("type")]
        public string Type { get; set; } = null!;
    }
}
