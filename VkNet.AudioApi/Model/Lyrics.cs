using Newtonsoft.Json;

namespace VkNet.AudioApi.Model
{
    public class Lyrics
    {
        [JsonProperty("md5")]
        public string Md5 { get; set; } = null!;

        [JsonProperty("lyrics")]
        public LyricsInfo LyricsInfo { get; set; } = null!;

        [JsonProperty("credits")]
        public string Credits { get; set; } = null!;
    }
}
