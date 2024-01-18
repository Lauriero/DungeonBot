using Newtonsoft.Json;

namespace VkNet.AudioApi.Model
{
    public class LyricsTimestamp
    {
        [JsonProperty("begin")]
        public int Begin { get; set; }

        [JsonProperty("end")]
        public int End { get; set; }

        [JsonProperty("line")]
        public string Line { get; set; } = null!;
    }
}
