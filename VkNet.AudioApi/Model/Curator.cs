using Newtonsoft.Json;

namespace VkNet.AudioApi.Model
{
    public class Curator
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; } = null!;

        [JsonProperty("description")]
        public string Description { get; set; } = null!;

        [JsonProperty("url")]
        public string Url { get; set; } = null!;

        [JsonProperty("photo")]
        public List<Image> Photo { get; set; } = null!;

        [JsonProperty("is_followed")]
        public bool IsFollowed { get; set; }

        [JsonProperty("can_follow")]
        public bool CanFollow { get; set; }
    }
}
