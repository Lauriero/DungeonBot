using Newtonsoft.Json;

namespace VkNet.AudioApi.Model
{
    public class Section
    {
        [JsonProperty("id")]
        public string Id { get; set; } = null!;

        [JsonProperty("title")]
        public string Title { get; set; } = null!;

        [JsonProperty("url")]
        public string Url { get; set; } = null!;

        [JsonProperty("blocks")]
        public List<Block> Blocks { get; set; } = new List<Block>();

        [JsonProperty("next_from")]
        public string NextFrom { get; set; } = null!;
    }
}
