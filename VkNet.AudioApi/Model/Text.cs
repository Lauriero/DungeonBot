using Newtonsoft.Json;

namespace VkNet.AudioApi.Model
{
    public class Text
    {
        [JsonProperty("id")]
        public string Id { get; set; } = null!;

        [JsonProperty("text")]
        public string Value { get; set; } = null!;

        [JsonProperty("collapsed_lines")]
        public int CollapsedLines { get; set; }
    }
}
