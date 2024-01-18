using Newtonsoft.Json;

namespace VkNet.AudioApi.Model
{
    public class OptionButton
    {
        [JsonProperty("replacement_id")]
        public string ReplacementId { get; set; } = null!;

        [JsonProperty("text")]
        public string Text { get; set; } = null!;

        [JsonProperty("icon")]
        public string Icon { get; set; } = null!;

        [JsonProperty("selected")]
        public int Selected { get; set; }
    }
}
