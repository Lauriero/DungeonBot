using Newtonsoft.Json;

namespace VkNet.AudioApi.Model
{
    public class Image
    {
        [JsonProperty("url")]
        public string Url { get; set; } = null!;

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }
    }
}
