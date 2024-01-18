using Newtonsoft.Json;

namespace VkNet.AudioApi.Model
{
    public class LyricsInfo
    {
        [JsonProperty("language")]
        public string Language { get; set; } = null!;

        [JsonProperty("timestamps")]
        public List<LyricsTimestamp> Timestamps { get; set; } = null!;

        [JsonProperty("text")]
        public List<string> Text { get; set; } = null!;
    }
}
