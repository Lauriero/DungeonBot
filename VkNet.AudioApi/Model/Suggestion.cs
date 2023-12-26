using Newtonsoft.Json;

namespace VkNet.AudioApi.Model
{
    public class Suggestion
    {
        [JsonProperty("id")]
        public string Id { get; set; } = null!;

        [JsonProperty("title")]
        public string Title { get; set; } = null!;

        [JsonProperty("subtitle")]
        public string Subtitle { get; set; } = null!;

        [JsonProperty("context")]
        public string Context { get; set; } = null!;
    }
}
