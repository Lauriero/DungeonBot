using Newtonsoft.Json;

namespace VkNet.AudioApi.Model
{
    public class CuratorGroup
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("track_code")]
        public string TrackCode { get; set; } = null!;
    }
}
