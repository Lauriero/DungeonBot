using Newtonsoft.Json;

namespace VkNet.AudioApi.Model
{
    public class MainArtist
    {
        [JsonProperty("name")]
        public string Name { get; set; } = null!;

        [JsonProperty("domain")]
        public string Domain { get; set; } = null!;

        [JsonProperty("id")]
        public string Id { get; set; } = null!;
    }
}
