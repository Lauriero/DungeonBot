using Newtonsoft.Json;

namespace VkNet.AudioApi.Model
{
    public class Badge
    {
        [JsonProperty("type")]
        public string Type { get; set; } = null!;

        [JsonProperty("text")]
        public string Text { get; set; } = null!;
    }
}
