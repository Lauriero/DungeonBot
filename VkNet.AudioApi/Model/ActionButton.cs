using Newtonsoft.Json;

namespace VkNet.AudioApi.Model
{
    public class ActionButton
    {
        [JsonProperty("type")]
        public string Type { get; set; } = null!;

        [JsonProperty("target")]
        public string Target { get; set; } = null!;

        [JsonProperty("url")]
        public string Url { get; set; } = null!;

        [JsonProperty("consume_reason")]
        public string ConsumeReason { get; set; } = null!;
    }
}
