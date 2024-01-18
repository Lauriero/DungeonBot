using Newtonsoft.Json;

namespace VkNet.AudioApi.Model
{
    public class TrackEvent
    {
        [JsonProperty("e")]
        public string Event { get; set; } = null!;

        [JsonProperty("uuid")]
        public int Uuid { get; set; }

    }
}
