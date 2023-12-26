using Newtonsoft.Json;

namespace VkNet.AudioApi.Model
{
    public class PodcastInfo
    {
        [JsonProperty("cover")]
        public Cover Cover { get; set; } = null!;

        [JsonProperty("description")]
        public string Description { get; set; } = null!;

        [JsonProperty("is_favorite")]
        public bool IsFavorite { get; set; }

        [JsonProperty("plays")]
        public int Plays { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }

        [JsonProperty("post")]
        public string Post { get; set; } = null!;
    }
}
